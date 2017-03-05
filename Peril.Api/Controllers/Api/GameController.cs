using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/Game")]
    public class GameController : ApiController
    {
        public GameController(ICommandQueue commandQueue, INationRepository nationRepository, IRegionRepository regionRepository, ISessionRepository sessionRepository, IUserRepository userRepository, IWorldRepository worldRepository)
        {
            CommandQueue = commandQueue;
            NationRepository = nationRepository;
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
            UserRepository = userRepository;
            WorldRepository = worldRepository;
        }

        // GET /api/Game/Sessions
        [Route("Sessions")]
        public async Task<IEnumerable<ISession>> GetSessions()
        {
            IEnumerable<ISession> sessionData = await SessionRepository.GetSessions();
            return from session in sessionData
                   select new Session(session);
        }

        // GET /api/Game/Session
        [Route("Session")]
        public async Task<ISession> GetSession(Guid sessionId)
        {
            return new Session(await SessionRepository.GetSessionOrThrow(sessionId));
        }

        // POST /api/Game/StartNewGame
        [Route("StartNewGame")]
        public async Task<ISession> PostStartNewSession(PlayerColour colour)
        {
            Guid sessionGuid = await SessionRepository.CreateSession(User.Identity.GetUserId(), colour);

            try
            {
                XDocument worldDefinition = XDocument.Load(RegionRepository.WorldDefinitionPath);
                List<Region> regions = worldDefinition.LoadRegions();
                worldDefinition.LoadRegionConnections(regions);

                List<Task> regionCreationOperations = new List<Task>();
                foreach(var region in regions)
                {
                    regionCreationOperations.Add(RegionRepository.CreateRegion(sessionGuid, region.RegionId, region.ContinentId, region.Name, region.ConnectedRegions));
                }
                await Task.WhenAll(regionCreationOperations);
            }
            catch (HttpResponseException exception)
            {
                throw exception;
            }
            catch (Exception)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, ReasonPhrase = "An exception occured while creating the regions" });
            }

            return new Models.Session { GameId = sessionGuid };
        }

        // POST /api/Game/JoinGame?gameId=guid-as-string
        [Route("JoinGame")]
        public async Task PostJoinSession(Guid sessionId, PlayerColour colour)
        {
            ISessionData session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseTypeOrThrow(SessionPhase.NotStarted);

            INationData nationData = await NationRepository.GetNation(sessionId, User.Identity.GetUserId());
            if (nationData == null)
            {
                try
                {
                    bool colourAvailable = await SessionRepository.ReservePlayerColour(session.GameId, session.CurrentEtag, colour);
                    if (colourAvailable)
                    {
                        await SessionRepository.JoinSession(sessionId, User.Identity.GetUserId(), colour);
                    }
                    else
                    {
                        throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotAcceptable, ReasonPhrase = "Another player has already taken that colour" });
                    }
                }
                catch (ConcurrencyException)
                {
                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Conflict, ReasonPhrase = "There was a concurrent access conflict" });
                }
            }
        }

        // GET /api/Game/Players
        [Route("Players")]
        public async Task<IEnumerable<IPlayer>> GetPlayers(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId);

            // Resolve player ids into IPlayer structures
            IEnumerable<INationData> nations = await NationRepository.GetNations(sessionId);
            return from nation in nations
                    join user in UserRepository.Users on nation.UserId equals user.Id
                    select new Player { UserId = nation.UserId, Name = user.UserName, Colour = nation.Colour };
        }

        // POST /api/Game/PostEndPhase
        [Route("EndPhase")]
        public async Task PostEndPhase(Guid sessionId, Guid phaseId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseIdOrThrow(phaseId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId());

            await NationRepository.MarkPlayerCompletedPhase(sessionId, User.Identity.GetUserId(), phaseId);
        }

        // POST /api/Game/PostAdvanceNextPhase
        [Route("AdvanceNextPhase")]
        public async Task PostAdvanceNextPhase(Guid sessionId, Guid phaseId, bool force)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseIdOrThrow(phaseId)
                                                      .IsSessionOwnerOrThrow(User.Identity.GetUserId());
            var nationsTask = NationRepository.GetNations(session.GameId);

            // Check all players ready (unless force == true)
            bool allPlayersReady = true;
            if (!force)
            {
                IEnumerable<INationData> nations = await nationsTask;
                foreach (INationData nation in nations)
                {
                    if (nation.CompletedPhase != session.PhaseId && nation.UserId != session.OwnerId)
                    {
                        allPlayersReady = false;
                        break;
                    }
                }
            }

            if (force || allPlayersReady)
            {
                SessionPhase nextPhase = SessionPhase.NotStarted;
                bool shouldSetPhaseAtEnd = true;

                using (IBatchOperationHandle batchOperation = SessionRepository.StartBatchOperation(session.GameId))
                {
                    // Run phase specific update logic
                    switch (session.PhaseType)
                    {
                        case SessionPhase.NotStarted:
                        {
                            var regions = RegionRepository.GetRegions(session.GameId);
                            var nations = await nationsTask;
                            DistributeInitialRegions(batchOperation, session.GameId, await regions, nations);
                            Dictionary<String, UInt32> initialReinforcements = new Dictionary<string, uint>();
                            foreach(INationData nation in nations)
                            {
                                // For the moment, just give all players 20 starting troops
                                initialReinforcements[nation.UserId] = 20;
                            }
                            NationRepository.SetAvailableReinforcements(batchOperation, session.GameId, initialReinforcements);
                            nextPhase = SessionPhase.Reinforcements;
                            break;
                        }
                        case SessionPhase.Reinforcements:
                        {
                            var regions = RegionRepository.GetRegions(session.GameId);
                            IEnumerable<ICommandQueueMessage> pendingMessages = await CommandQueue.GetQueuedCommands(session.GameId, session.PhaseId);
                            IEnumerable<IDeployReinforcementsMessage> pendingReinforcementMessages = pendingMessages.GetQueuedDeployReinforcementsCommands();

                            IEnumerable<INationData> nations = await nationsTask;
                            ProcessReinforcementMessages(batchOperation, session.GameId, await regions, nations, pendingReinforcementMessages);

                            // Reset all players to 0 reinforcements
                            Dictionary<String, UInt32> initialReinforcements = new Dictionary<string, uint>();
                            foreach (INationData nation in nations)
                            {
                                initialReinforcements[nation.UserId] = 0;
                            }

                            NationRepository.SetAvailableReinforcements(batchOperation, session.GameId, initialReinforcements);
                            CommandQueue.RemoveCommands(batchOperation, session.GameId, pendingMessages);
                            nextPhase = SessionPhase.CombatOrders;
                            break;
                        }
                        case SessionPhase.CombatOrders:
                        {
                            var regions = RegionRepository.GetRegions(session.GameId);
                            var borderClashesTask = WorldRepository.GetCombat(session.GameId, session.Round, CombatType.BorderClash);
                            IEnumerable<ICommandQueueMessage> pendingMessages = await CommandQueue.GetQueuedCommands(session.GameId, session.PhaseId);
                            IEnumerable<IOrderAttackMessage> pendingAttackMessages = pendingMessages.GetQueuedOrderAttacksCommands();

                            nextPhase = ProcessCombatOrders(batchOperation, session.GameId, session.Round, await regions, await nationsTask, await borderClashesTask, pendingAttackMessages);
                            if(nextPhase == SessionPhase.CombatOrders)
                            {
                                shouldSetPhaseAtEnd = false;
                            }
                            break;
                        }
                        case SessionPhase.BorderClashes:
                        {
                            var borderClashes = WorldRepository.GetCombat(session.GameId, session.Round, CombatType.BorderClash);
                            IEnumerable<CombatResult> results = await ResolveCombat(session.GameId, session.Round, await borderClashes);
                            await ApplyBorderClashResults(session.GameId, session.Round, results);
                            nextPhase = SessionPhase.MassInvasions;
                            break;
                        }
                        case SessionPhase.MassInvasions:
                        {
                            var massInvasions = WorldRepository.GetCombat(session.GameId, session.Round, CombatType.MassInvasion);
                            IEnumerable<CombatResult> results = await ResolveCombat(session.GameId, session.Round, await massInvasions);
                            ApplyCombatResults(batchOperation, session.GameId, session.Round, CombatType.MassInvasion, results);
                            nextPhase = SessionPhase.Invasions;
                            break;
                        }
                        case SessionPhase.Invasions:
                        {
                            var invasions = WorldRepository.GetCombat(session.GameId, session.Round, CombatType.Invasion);
                            IEnumerable<CombatResult> results = await ResolveCombat(session.GameId, session.Round, await invasions);
                            ApplyCombatResults(batchOperation, session.GameId, session.Round, CombatType.Invasion, results);
                            nextPhase = SessionPhase.SpoilsOfWar;
                            break;
                        }
                        case SessionPhase.SpoilsOfWar:
                        {
                            var invasions = WorldRepository.GetCombat(session.GameId, session.Round, CombatType.SpoilsOfWar);
                            IEnumerable<CombatResult> results = await ResolveCombat(session.GameId, session.Round, await invasions);
                            ApplyCombatResults(batchOperation, session.GameId, session.Round, CombatType.SpoilsOfWar, results);
                            nextPhase = SessionPhase.Redeployment;
                            break;
                        }
                        case SessionPhase.Redeployment:
                        {
                            // For DevoLAN 31, we're skipping redeployment!
                            nextPhase = SessionPhase.Victory;
                            break;
                        }
                        case SessionPhase.Victory:
                        {
                            // Award reinforcements
                            var regions = RegionRepository.GetRegions(session.GameId);
                            await AwardReinforcements(sessionId, await regions);
                            nextPhase = SessionPhase.Reinforcements;
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException("Not done yet");
                        }
                    }

                    // Must commit the batch prior to kicking off the operation to set the session phase
                    await batchOperation.CommitBatch();
                }

                if(shouldSetPhaseAtEnd)
                {
                    await SessionRepository.SetSessionPhase(sessionId, session.PhaseId, nextPhase);
                }
            }
            else
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = "Some players are not yet ready and a forced operation was not requested" });
            }
        }

        private void DistributeInitialRegions(IBatchOperationHandle batchOperation, Guid sessionId, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> availablePlayers)
        {
            Dictionary<Guid, OwnershipChange> assignedRegions = new Dictionary<Guid, OwnershipChange>();

            // Calculate how many regions each player should get and how many (if any) extra regions there will be
            int numberOfRegions = availableRegions.Count();
            int numberOfPlayers = availablePlayers.Count();
            int minimumNumberOfRegionsPerPlayer = numberOfRegions / numberOfPlayers;
            int extraRegions = numberOfRegions - (minimumNumberOfRegionsPerPlayer * numberOfPlayers);
            Random random = new Random();

            // Build a list of regions to be distributed, ordered by their continent size (largest first)
            var regionsGroupedByContinent = from regionData in availableRegions
                                            group regionData by regionData.ContinentId into continent
                                            orderby continent.Count() descending
                                            select continent;

            List<IRegionData> regionsToBeAssigned = new List<IRegionData>();
            foreach(var continent in regionsGroupedByContinent)
            {
                List<IRegionData> continentRegions = continent.ToList();
                continentRegions.Shuffle(random);
                regionsToBeAssigned.AddRange(continentRegions);
            }

            // Distribute "extra" regions. These will be on the largest continent, so this should balance the fact you get an extra region
            List<INationData> playersToBeAssigned = availablePlayers.ToList();
            playersToBeAssigned.Shuffle(random);
            int regionIndex = 0;
            for(int counter = 0; counter < extraRegions; ++counter, ++regionIndex)
            {
                IRegionData region = regionsToBeAssigned[regionIndex];
                INationData player = playersToBeAssigned[counter];
                assignedRegions[region.RegionId] = new OwnershipChange(player.UserId, 1);
            }

            // Distribute the remaining regions in a round robin fashion
            for (int roundCounter = 0; roundCounter < minimumNumberOfRegionsPerPlayer; ++roundCounter)
            {
                playersToBeAssigned.Shuffle(random);
                for (int playerCounter = 0; playerCounter < playersToBeAssigned.Count; ++playerCounter, ++regionIndex)
                {
                    IRegionData region = regionsToBeAssigned[regionIndex];
                    INationData player = playersToBeAssigned[playerCounter];
                    assignedRegions[region.RegionId] = new OwnershipChange(player.UserId, 1);
                }
            }

            RegionRepository.AssignRegionOwnership(batchOperation, sessionId, assignedRegions);
        }

        private void ProcessReinforcementMessages(IBatchOperationHandle batchOperation, Guid sessionId, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> players, IEnumerable<IDeployReinforcementsMessage> messages)
        {
            Dictionary<Guid, OwnershipChange> assignedRegions = new Dictionary<Guid, OwnershipChange>();
            Dictionary<String, UInt32> playerLookup = players.ToDictionary(nationData => nationData.UserId, nationData => nationData.AvailableReinforcements);
            Dictionary<Guid, IRegionData> regionLookup = availableRegions.ToDictionary(regionData => regionData.RegionId);

            foreach (IDeployReinforcementsMessage message in messages)
            {
                // Sanity check: Ignore messages for unknown regions & players
                if(regionLookup.ContainsKey(message.TargetRegion))
                {
                    IRegionData regionData = regionLookup[message.TargetRegion];

                    // Sanity check: Region ETag must still match
                    if (regionData.CurrentEtag == message.TargetRegionEtag)
                    {
                        // Sanity check: Player must have enough troops remaining
                        if (playerLookup[regionData.OwnerId] >= message.NumberOfTroops)
                        {
                            if (assignedRegions.ContainsKey(regionData.RegionId))
                            {
                                // Already an entry for this region, so merge
                                assignedRegions[regionData.RegionId].TroopCount += message.NumberOfTroops;
                            }
                            else
                            {
                                // New entry for this region, so add
                                assignedRegions[regionData.RegionId] = new OwnershipChange(regionData.OwnerId, regionData.TroopCount + message.NumberOfTroops);
                            }

                            playerLookup[regionData.OwnerId] -= message.NumberOfTroops;
                        }
                    }
                }
            }

            if (assignedRegions.Count > 0)
            {
                RegionRepository.AssignRegionOwnership(batchOperation, sessionId, assignedRegions);
            }
        }

        private SessionPhase ProcessCombatOrders(IBatchOperationHandle batchOperation, Guid sessionId, UInt32 round, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> players, IEnumerable<ICombat> existingBorderClashes,IEnumerable<IOrderAttackMessage> messages)
        {
            SessionPhase nextSessionPhase = SessionPhase.Redeployment;
            Dictionary<Guid, OwnershipChange> regionOwnershipChanges = new Dictionary<Guid, OwnershipChange>();

            // Track how many troops are available in each region
            var sourceRegionsQuery = from region in availableRegions
                                     select new CombatOrderRegion { RegionId = region.RegionId, TroopCount = region.TroopCount, CurrentEtag = region.CurrentEtag, OwnerId = region.OwnerId };
            var sourceRegions = sourceRegionsQuery.ToDictionary(entry => entry.RegionId);


            // Group all orders by target region (process most attacked targets first)
            var attacksBySourceRegionQuery = from message in messages
                                             group message by message.SourceRegion into source
                                             select source;
            var attacksBySourceRegion = attacksBySourceRegionQuery.ToDictionary(entry => entry.Key, entry => entry.ToList());

            // Build up a list of all the outgoing armies
            foreach(var sourceRegionMessages in attacksBySourceRegion)
            {
                // Ignore any invalid messages that don't come from a region that actually exists
                if (sourceRegions.ContainsKey(sourceRegionMessages.Key))
                {
                    var regionData = sourceRegions[sourceRegionMessages.Key];
                    foreach(IOrderAttackMessage attackMessage in sourceRegionMessages.Value)
                    {
                        if(!regionData.OutgoingArmies.ContainsKey(attackMessage.TargetRegion))
                        {
                            regionData.OutgoingArmies[attackMessage.TargetRegion] = new List<IOrderAttackMessage>();
                        }
                        regionData.OutgoingArmies[attackMessage.TargetRegion].Add(attackMessage);
                    }
                }
                else
                {
                    if (batchOperation.RemainingCapacity <= sourceRegionMessages.Value.Count)
                    {
                        return SessionPhase.CombatOrders;
                    }

                    // Remove all attack commands for the invalid region
                    CommandQueue.RemoveCommands(batchOperation, sessionId, sourceRegionMessages.Value);
                }
            }

            var resolvedCombat = new List<Tuple<CombatType, IEnumerable<ICombatArmy>>>();

            // Detect border clashes
            Dictionary<Guid, List<Guid>> regionAttackers = new Dictionary<Guid, List<Guid>>();
            foreach (var regionEntry in sourceRegions)
            {
                Guid sourceRegionId = regionEntry.Key;
                var sourceRegionData = regionEntry.Value;

                List<Guid> targetRegions = regionEntry.Value.OutgoingArmies.Keys.ToList();
                foreach(var targetRegionId in targetRegions)
                {
                    // Skip this army if the troop count is 0 (means we've already created a border clash)
                    if (sourceRegionData.OutgoingArmies[targetRegionId].Count > 0 && sourceRegions.ContainsKey(targetRegionId))
                    {
                        var targetRegionData = sourceRegions[targetRegionId];
                        if (targetRegionData.OutgoingArmies.ContainsKey(sourceRegionId))
                        {
                            // Ensure we have enough batch capacity remaining to process this combat.
                            // 2 to modify the remaining troop count, 1 to create the combat, plus enough to delete all the involved messsages
                            int batchCapacityRequired = regionOwnershipChanges.Count + 2 + resolvedCombat.Count + 1 + sourceRegionData.OutgoingArmies[targetRegionId].Count + targetRegionData.OutgoingArmies[sourceRegionId].Count;
                            if(batchCapacityRequired > batchOperation.RemainingCapacity)
                            {
                                // Store combat
                                WorldRepository.AddCombat(batchOperation, sessionId, round, resolvedCombat);

                                // Update source regions with new troop levels
                                RegionRepository.AssignRegionOwnership(batchOperation, sessionId, regionOwnershipChanges);

                                // Return that the combat orders phase needs another pass to complete
                                return SessionPhase.CombatOrders;
                            }

                            // Work out troops involved by merging attacks
                            var troopsFromSourceRegionQuery = from outgoingArmy in sourceRegionData.OutgoingArmies[targetRegionId]
                                                                select outgoingArmy.NumberOfTroops;
                            UInt32 troopsFromSourceRegion = Math.Min((UInt32)troopsFromSourceRegionQuery.Sum(entry => entry), sourceRegionData.TroopCount - 1);
                            var troopsFromTargetRegionQuery = from outgoingArmy in targetRegionData.OutgoingArmies[sourceRegionId]
                                                              select outgoingArmy.NumberOfTroops;
                            UInt32 troopsFromTargetRegion = Math.Min((UInt32)troopsFromTargetRegionQuery.Sum(entry => entry), targetRegionData.TroopCount - 1);

                            // Create Border clash
                            IEnumerable<ICombatArmy> involvedArmies = new List<CombatArmy>
                            {
                                new CombatArmy(targetRegionId, targetRegionData.OwnerId, CombatArmyMode.Attacking, troopsFromTargetRegion),
                                new CombatArmy(sourceRegionId, sourceRegionData.OwnerId, CombatArmyMode.Attacking, troopsFromSourceRegion)
                            };
                            resolvedCombat.Add(Tuple.Create(CombatType.BorderClash, involvedArmies));

                            // Queue up removal of all involved messages
                            CommandQueue.RemoveCommands(batchOperation, sessionId, targetRegionData.OutgoingArmies[sourceRegionId]);
                            targetRegionData.OutgoingArmies[sourceRegionId].Clear();
                            CommandQueue.RemoveCommands(batchOperation, sessionId, sourceRegionData.OutgoingArmies[targetRegionId]);
                            sourceRegionData.OutgoingArmies[targetRegionId].Clear();

                            // Modify involved regions to reduce their remaining troop count
                            if (regionOwnershipChanges.ContainsKey(sourceRegionData.RegionId))
                            {
                                regionOwnershipChanges[sourceRegionData.RegionId].TroopCount -= troopsFromSourceRegion;
                            }
                            else
                            {
                                regionOwnershipChanges.Add(sourceRegionData.RegionId, new OwnershipChange(sourceRegionData.OwnerId, sourceRegionData.TroopCount - troopsFromSourceRegion));
                            }
                            sourceRegionData.TroopCount -= troopsFromSourceRegion;

                            if (regionOwnershipChanges.ContainsKey(targetRegionData.RegionId))
                            {
                                regionOwnershipChanges[targetRegionData.RegionId].TroopCount -= troopsFromTargetRegion;
                            }
                            else
                            {
                                regionOwnershipChanges.Add(targetRegionData.RegionId, new OwnershipChange(targetRegionData.OwnerId, targetRegionData.TroopCount - troopsFromTargetRegion));
                            }
                            targetRegionData.TroopCount -= troopsFromTargetRegion;

                            // There will be at least one border clash to resolve
                            nextSessionPhase = SessionPhase.BorderClashes;
                        }
                    }

                    if(!regionAttackers.ContainsKey(targetRegionId))
                    {
                        regionAttackers[targetRegionId] = new List<Guid>();
                    }
                    regionAttackers[targetRegionId].Add(sourceRegionId);
                }
            }

            foreach(ICombat borderClash in existingBorderClashes)
            {
                if(borderClash.InvolvedArmies.Count() == 2)
                {
                    ICombatArmy firstArmy = borderClash.InvolvedArmies.ElementAt(0);
                    ICombatArmy secondArmy = borderClash.InvolvedArmies.ElementAt(1);

                    if (!regionAttackers.ContainsKey(firstArmy.OriginRegionId))
                    {
                        regionAttackers[firstArmy.OriginRegionId] = new List<Guid>();
                    }
                    regionAttackers[firstArmy.OriginRegionId].Add(secondArmy.OriginRegionId);

                    if (!regionAttackers.ContainsKey(secondArmy.OriginRegionId))
                    {
                        regionAttackers[secondArmy.OriginRegionId] = new List<Guid>();
                    }
                    regionAttackers[secondArmy.OriginRegionId].Add(firstArmy.OriginRegionId);
                }
            }

            // Detect invasions & mass invasions. We can only detect "mass" invasions correctly by grouping the attacks by their target rather than source
            foreach (var targetRegionPair in regionAttackers)
            {
                var combatOrdersInvolvedQuery = from atackingRegionId in targetRegionPair.Value
                                                let attackingRegionData = sourceRegions[atackingRegionId]
                                                where attackingRegionData.OutgoingArmies.ContainsKey(targetRegionPair.Key)
                                                from outgoingArmy in attackingRegionData.OutgoingArmies[targetRegionPair.Key]
                                                select outgoingArmy;
                var combatOrdersInvolved = combatOrdersInvolvedQuery.ToList();

                // Ensure we have enough batch capacity remaining to process this combat.
                // 1 to create the combat and 1 per attacker involved (to modify their region) and enough to delete all the involved messages
                int batchCapacityRequired = regionOwnershipChanges.Count + targetRegionPair.Value.Count + resolvedCombat.Count + 1 + combatOrdersInvolved.Count();
                if (batchCapacityRequired > batchOperation.RemainingCapacity)
                {
                    // Store combat
                    WorldRepository.AddCombat(batchOperation, sessionId, round, resolvedCombat);

                    // Update source regions with new troop levels
                    RegionRepository.AssignRegionOwnership(batchOperation, sessionId, regionOwnershipChanges);

                    // Return that the combat orders phase needs another pass to complete
                    return SessionPhase.CombatOrders;
                }

                CombatType combatType = CombatType.Invasion;
                if(targetRegionPair.Value.Count > 1)
                {
                    combatType = CombatType.MassInvasion;
                }

                List<ICombatArmy> involvedArmies = new List<ICombatArmy>();

                // Add attacking armies. Ignore any that are currently in a BorderClash
                foreach (Guid sourceRegionId in targetRegionPair.Value)
                {
                    var sourceRegionData = sourceRegions[sourceRegionId];
                    if (sourceRegionData.OutgoingArmies.ContainsKey(targetRegionPair.Key))
                    {
                        var troopsFromSourceRegionQuery = from outgoingArmy in sourceRegionData.OutgoingArmies[targetRegionPair.Key]
                                                          select outgoingArmy.NumberOfTroops;
                        UInt32 attackingArmySize = Math.Min((UInt32)troopsFromSourceRegionQuery.Sum(entry => entry), sourceRegionData.TroopCount - 1);
                        if (attackingArmySize > 0)
                        {
                            involvedArmies.Add(new CombatArmy(sourceRegionId, sourceRegionData.OwnerId, CombatArmyMode.Attacking, attackingArmySize));

                            // Modify attacking region to reduce their remaining troop count
                            if (regionOwnershipChanges.ContainsKey(sourceRegionData.RegionId))
                            {
                                regionOwnershipChanges[sourceRegionData.RegionId].TroopCount -= attackingArmySize;
                            }
                            else
                            {
                                regionOwnershipChanges.Add(sourceRegionData.RegionId, new OwnershipChange(sourceRegionData.OwnerId, sourceRegionData.TroopCount - attackingArmySize));
                            }
                            sourceRegionData.TroopCount -= attackingArmySize;
                        }
                        sourceRegionData.OutgoingArmies[targetRegionPair.Key].Clear();
                    }
                }

                // Add a combat even if all attackers are involved in border clashes (we'll skip it later)
                // Add the defending army
                var defendingRegionData = sourceRegions[targetRegionPair.Key];
                var troopsRemainingInDefendingRegionQuery = from outgoingArmyByTargetRegion in defendingRegionData.OutgoingArmies
                                                            from outgoingArmy in outgoingArmyByTargetRegion.Value
                                                            select outgoingArmy.NumberOfTroops;
                UInt32 troopsLeavingDefendingRegion = Math.Min((UInt32)troopsRemainingInDefendingRegionQuery.Sum(entry => entry), defendingRegionData.TroopCount - 1);
                involvedArmies.Add(new CombatArmy(targetRegionPair.Key, defendingRegionData.OwnerId, CombatArmyMode.Defending, defendingRegionData.TroopCount - troopsLeavingDefendingRegion));

                resolvedCombat.Add(Tuple.Create(combatType, involvedArmies.AsEnumerable()));

                CommandQueue.RemoveCommands(batchOperation, sessionId, combatOrdersInvolved);

                // Update the next session phase if required
                if(nextSessionPhase == SessionPhase.Redeployment || nextSessionPhase == SessionPhase.Invasions)
                {
                    nextSessionPhase = combatType == CombatType.MassInvasion ? SessionPhase.MassInvasions : SessionPhase.Invasions;
                }
            }

            if (resolvedCombat.Count > 0)
            {
                // Store combat
                WorldRepository.AddCombat(batchOperation, sessionId, round, resolvedCombat);

                // Update source regions with new troop levels
                RegionRepository.AssignRegionOwnership(batchOperation, sessionId, regionOwnershipChanges);
            }

            return nextSessionPhase;
        }

        private async Task<IEnumerable<CombatResult>> ResolveCombat(Guid sessionId, UInt32 round, IEnumerable<ICombat> pendingCombat)
        {
            List<CombatResult> combatResults = new List<CombatResult>();
            foreach(ICombat combat in pendingCombat)
            {
                combatResults.Add(CombatResult.GenerateForCombat(combat, (Guid regionId) => WorldRepository.GetRandomNumberGenerator(regionId, 1, 6).Select(value => (UInt32)value)));
            }

            if (combatResults.Count > 0)
            {
                await WorldRepository.AddCombatResults(sessionId, round, combatResults);
            }

            return combatResults;
        }

        private async Task ApplyBorderClashResults(Guid sessionId, UInt32 round, IEnumerable<CombatResult> combatResults)
        {
            Dictionary<Guid, IEnumerable<ICombatArmy>> survivingArmies = new Dictionary<Guid, IEnumerable<ICombatArmy>>();

            foreach(CombatResult result in combatResults)
            {
                if(result.SurvivingArmies.Count() == 1)
                {
                    // Figure out which side lost
                    var survivingArmy = result.SurvivingArmies.First();
                    var defeatedArmy = result.StartingArmies.Where(army => army.OriginRegionId != survivingArmy.OriginRegionId).ToList();
                    if(defeatedArmy.Count == 1)
                    {
                        survivingArmies[defeatedArmy[0].OriginRegionId] = new List<ICombatArmy> { survivingArmy };
                    }
                }
            }

            await WorldRepository.AddArmyToCombat(sessionId, round, CombatType.BorderClash, survivingArmies);
        }

        private void ApplyCombatResults(IBatchOperationHandle batchOperationHandle, Guid sessionId, UInt32 round, CombatType type, IEnumerable<CombatResult> combatResults)
        {
            Dictionary<Guid, OwnershipChange> regionOwnershipChanges = new Dictionary<Guid, OwnershipChange>();
            List<Tuple<CombatType, IEnumerable<ICombatArmy>>> spoilsOfWar = new List<Tuple<CombatType, IEnumerable<ICombatArmy>>>();

            foreach (CombatResult result in combatResults)
            {
                var survivingNationsQuery = from army in result.SurvivingArmies
                                            group army by army.OwnerUserId into survivingOwnerIds
                                            select survivingOwnerIds;
                var defendingArmy = result.StartingArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();

                if (defendingArmy != null)
                {
                    if (survivingNationsQuery.Count() == 1)
                    {
                        // Figure out which side lost

                        Guid battleRegionId = defendingArmy.OriginRegionId;
                        UInt32 survivingTroops = 0;
                        foreach (CombatArmy army in result.SurvivingArmies)
                        {
                            survivingTroops += army.NumberOfTroops;
                        }

                        regionOwnershipChanges[battleRegionId] = new OwnershipChange(survivingNationsQuery.First().Key, survivingTroops);
                    }
                    else
                    {
                        var survivingArmies = result.SurvivingArmies.ToList();

                        // Add an empty defending army, so we know which region is being attacked
                        survivingArmies.Add(new CombatArmy(defendingArmy.OriginRegionId, defendingArmy.OwnerUserId, CombatArmyMode.Defending, 0));
    
                        spoilsOfWar.Add(Tuple.Create<CombatType, IEnumerable<ICombatArmy>>(CombatType.SpoilsOfWar, survivingArmies));
                    }
                }
            }

            if (regionOwnershipChanges.Count > 0)
            {
                RegionRepository.AssignRegionOwnership(batchOperationHandle, sessionId, regionOwnershipChanges);
            }

            if(spoilsOfWar.Count > 0)
            {
                WorldRepository.AddCombat(batchOperationHandle, sessionId, round, spoilsOfWar);
            }
        }

        private async Task AwardReinforcements(Guid sessionId, IEnumerable<IRegionData> regions)
        {
            var regionByPlayerQuery = from region in regions
                                      group region by region.OwnerId into regionsPerNation
                                      select regionsPerNation;

            Dictionary < String, UInt32> reinforcements = new Dictionary<string, uint>();
            foreach (var playerWithRegions in regionByPlayerQuery)
            {
                // Award player a reinforcement for every 3 regions they own, minimum of at least 3 reinforcements
                reinforcements[playerWithRegions.Key] = (UInt32)Math.Max(3, playerWithRegions.Count() / 3);
            }

            var regionByContinent = from region in regions
                                    group region by region.ContinentId into continent
                                    select continent;
            foreach(var continent in regionByContinent)
            {
                var ownersInContinent = from region in continent
                                        group region by region.OwnerId into owner
                                        select owner;
                if(ownersInContinent.Count() == 1)
                {
                    reinforcements[ownersInContinent.First().Key] += 5;
                }
            }

            using (IBatchOperationHandle batchOperation = SessionRepository.StartBatchOperation(sessionId))
            {
                NationRepository.SetAvailableReinforcements(batchOperation, sessionId, reinforcements);
                await batchOperation.CommitBatch();
            }
        }

        private ICommandQueue CommandQueue { get; set; }
        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
        private IWorldRepository WorldRepository { get; set; }
    }
}
