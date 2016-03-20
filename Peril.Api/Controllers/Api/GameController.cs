﻿using Microsoft.AspNet.Identity;
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
                var regionsList = from continentXml in worldDefinition.Root.Elements("Continent")
                                  let continentId = Guid.NewGuid()
                                  from regionXml in continentXml.Elements("Region")
                                  select new Region
                                  {
                                      RegionId = Guid.NewGuid(),
                                      ContinentId = continentId,
                                      Name = regionXml.Attribute("Name").Value
                                  };
                List<Region> regions = regionsList.ToList();

                Dictionary<String, Region> regionLookup = new Dictionary<string, Region>();
                Dictionary<String, List<Guid>> regionConnectionsLookup = new Dictionary<string, List<Guid>>();
                foreach (Region region in regions)
                {
                    regionLookup[region.Name] = region;
                    regionConnectionsLookup[region.Name] = new List<Guid>();
                }

                var connections = from connectionXml in worldDefinition.Root.Elements("Connections")
                                  from connectedXml in connectionXml.Elements("Connected")
                                  let regionId = connectedXml.Attribute("Name").Value
                                  let otherRegionId = connectedXml.Attribute("Other").Value
                                  join regionData in regions on regionId equals regionData.Name
                                  join otherRegionData in regions on otherRegionId equals otherRegionData.Name
                                  select new
                                  {
                                      Region = regionId,
                                      RegionId = regionData.RegionId,
                                      OtherRegion = otherRegionId,
                                      OtherRegionId = otherRegionData.RegionId
                                  };

                
                foreach(var connection in connections)
                {
                    regionConnectionsLookup[connection.Region].Add(connection.OtherRegionId);
                    regionConnectionsLookup[connection.OtherRegion].Add(connection.RegionId);
                }

                List<Task> regionCreationOperations = new List<Task>();
                foreach(var regionPair in regionLookup)
                {
                    regionCreationOperations.Add(RegionRepository.CreateRegion(sessionGuid, regionPair.Value.RegionId, regionPair.Value.ContinentId, regionPair.Key, regionConnectionsLookup[regionPair.Key]));
                }
                await Task.WhenAll(regionCreationOperations);
            }
            catch(Exception)
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

            // Check all players ready (unless force == true)

            // Run phase specific update logic
            SessionPhase nextPhase = SessionPhase.NotStarted;
            switch (session.PhaseType)
            {
                case SessionPhase.NotStarted:
                {
                    var regions = RegionRepository.GetRegions(session.GameId);
                    var nations = await NationRepository.GetNations(session.GameId);
                    await DistributeInitialRegions(session.GameId, await regions, nations);
                    Dictionary<String, UInt32> initialReinforcements = new Dictionary<string, uint>();
                    foreach(INationData nation in nations)
                    {
                        // For the moment, just give all players 20 starting troops
                        initialReinforcements[nation.UserId] = 20;
                    }
                    await NationRepository.SetAvailableReinforcements(session.GameId, initialReinforcements);
                    nextPhase = SessionPhase.Reinforcements;
                    break;
                }
                case SessionPhase.Reinforcements:
                {
                    var regions = RegionRepository.GetRegions(session.GameId);
                    var nationsTask = NationRepository.GetNations(session.GameId);
                    IEnumerable<ICommandQueueMessage> pendingMessages = await CommandQueue.GetQueuedCommands(session.GameId, session.PhaseId);
                    IEnumerable<IDeployReinforcementsMessage> pendingReinforcementMessages = pendingMessages.GetQueuedDeployReinforcementsCommands();

                    IEnumerable<INationData> nations = await nationsTask;
                    await ProcessReinforcementMessages(session.GameId, await regions, nations, pendingReinforcementMessages);

                    // Reset all players to 0 reinforcements
                    Dictionary<String, UInt32> initialReinforcements = new Dictionary<string, uint>();
                    foreach (INationData nation in nations)
                    {
                        initialReinforcements[nation.UserId] = 0;
                    }
                    await NationRepository.SetAvailableReinforcements(session.GameId, initialReinforcements);
                    await CommandQueue.RemoveCommands(session.PhaseId);
                    nextPhase = SessionPhase.CombatOrders;
                    break;
                }
                case SessionPhase.CombatOrders:
                {
                    var regions = RegionRepository.GetRegions(session.GameId);
                    var nationsTask = NationRepository.GetNations(session.GameId);
                    IEnumerable<ICommandQueueMessage> pendingMessages = await CommandQueue.GetQueuedCommands(session.GameId, session.PhaseId);
                    IEnumerable<IOrderAttackMessage> pendingAttackMessages = pendingMessages.GetQueuedOrderAttacksCommands();
                    nextPhase = await ProcessCombatOrders(session.GameId, await regions, await nationsTask, pendingAttackMessages);
                    await CommandQueue.RemoveCommands(session.PhaseId);
                    break;
                }
                case SessionPhase.BorderClashes:
                {
                    var borderClashes = WorldRepository.GetCombat(session.GameId, CombatType.BorderClash);
                    IEnumerable<CombatResult> results = await ResolveCombat(session.GameId, await borderClashes);
                    await ApplyBorderClashResults(session.GameId, results);
                    nextPhase = SessionPhase.MassInvasions;
                    break;
                }
                default:
                {
                    throw new NotImplementedException("Not done yet");
                }
            }

            // Move to next phase
            await SessionRepository.SetSessionPhase(sessionId, session.PhaseId, nextPhase);
        }

        private async Task DistributeInitialRegions(Guid sessionId, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> availablePlayers)
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

            await RegionRepository.AssignRegionOwnership(sessionId, assignedRegions);
        }

        private async Task ProcessReinforcementMessages(Guid sessionId, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> players, IEnumerable<IDeployReinforcementsMessage> messages)
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
                await RegionRepository.AssignRegionOwnership(sessionId, assignedRegions);
            }
        }

        private async Task<SessionPhase> ProcessCombatOrders(Guid sessionId, IEnumerable<IRegionData> availableRegions, IEnumerable<INationData> players, IEnumerable<IOrderAttackMessage> messages)
        {
            SessionPhase nextSessionPhase = SessionPhase.Redeployment;

            // Track how many troops are available in each region
            var sourceRegionsQuery = from region in availableRegions
                                     select new CombatOrderRegion { RegionId = region.RegionId, TroopCount = region.TroopCount, CurrentEtag = region.CurrentEtag, OwnerId = region.OwnerId };
            var sourceRegions = sourceRegionsQuery.ToDictionary(entry => entry.RegionId);


            // Group all orders by target region (process most attacked targets first)
            var attacksBySourceRegionQuery = from message in messages
                                             group message by message.SourceRegion into source
                                             select source;
            var attacksBySourceRegion = attacksBySourceRegionQuery.ToDictionary(entry => entry.Key, entry => entry.ToList());

            // Group duplicate source regions
            foreach(var sourceRegionMessages in attacksBySourceRegion)
            {
                // Ignore any invalid messages that don't come from a region that actually exists
                if (sourceRegions.ContainsKey(sourceRegionMessages.Key))
                {
                    var regionData = sourceRegions[sourceRegionMessages.Key];
                    foreach(IOrderAttackMessage attackMessage in sourceRegionMessages.Value)
                    {
                        // Ignore any messages that don't match the Etag
                        if(attackMessage.SourceRegionEtag == regionData.CurrentEtag)
                        {
                            // Ignore any messages that would reduce the number of available troops below 1
                            if(regionData.TroopCount > attackMessage.NumberOfTroops)
                            {
                                if(!regionData.OutgoingArmies.ContainsKey(attackMessage.TargetRegion))
                                {
                                    regionData.OutgoingArmies[attackMessage.TargetRegion] = 0;
                                }
                                regionData.OutgoingArmies[attackMessage.TargetRegion] += attackMessage.NumberOfTroops;
                                regionData.TroopCount -= attackMessage.NumberOfTroops;
                            }
                        }
                    }
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
                    if (sourceRegionData.OutgoingArmies[targetRegionId] > 0 && sourceRegions.ContainsKey(targetRegionId))
                    {
                        var targetRegionData = sourceRegions[targetRegionId];
                        if (targetRegionData.OutgoingArmies.ContainsKey(sourceRegionId))
                        {
                            // Border clash!
                            IEnumerable<ICombatArmy> involvedArmies = new List<CombatArmy>
                            {
                                new CombatArmy(targetRegionId, targetRegionData.OwnerId, CombatArmyMode.Attacking, targetRegionData.OutgoingArmies[sourceRegionId]),
                                new CombatArmy(sourceRegionId, sourceRegionData.OwnerId, CombatArmyMode.Attacking, sourceRegionData.OutgoingArmies[targetRegionId])
                            };
                            resolvedCombat.Add(Tuple.Create(CombatType.BorderClash, involvedArmies));

                            // Clear the number of attacking troops, so we know that we've processed this already
                            targetRegionData.OutgoingArmies[sourceRegionId] = 0;
                            sourceRegionData.OutgoingArmies[targetRegionId] = 0;

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

            // Detect invasions & mass invasions. We can only detect "mass" invasions correctly by grouping the attacks by their target rather than source
            foreach (var targetRegionPair in regionAttackers)
            {
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
                    UInt32 attackingArmySize = sourceRegionData.OutgoingArmies[targetRegionPair.Key];
                    if (attackingArmySize > 0)
                    {
                        involvedArmies.Add(new CombatArmy(sourceRegionId, sourceRegionData.OwnerId, CombatArmyMode.Attacking, attackingArmySize));
                    }
                }

                // Add a combat even if all attackers are involved in border clashes (we'll skip it later)
                // Add the defending army
                var defendingRegionData = sourceRegions[targetRegionPair.Key];
                involvedArmies.Add(new CombatArmy(targetRegionPair.Key, defendingRegionData.OwnerId, CombatArmyMode.Defending, defendingRegionData.TroopCount));

                resolvedCombat.Add(Tuple.Create(combatType, involvedArmies.AsEnumerable()));

                // Update the next session phase if required
                if(nextSessionPhase == SessionPhase.Redeployment || nextSessionPhase == SessionPhase.Invasions)
                {
                    nextSessionPhase = combatType == CombatType.MassInvasion ? SessionPhase.MassInvasions : SessionPhase.Invasions;
                }
            }

            // Store combat
            await WorldRepository.AddCombat(sessionId, resolvedCombat);

            return nextSessionPhase;
        }

        private async Task<IEnumerable<CombatResult>> ResolveCombat(Guid sessionId, IEnumerable<ICombat> pendingCombat)
        {
            List<CombatResult> combatResults = new List<CombatResult>();
            foreach(ICombat combat in pendingCombat)
            {
                combatResults.Add(CombatResult.GenerateForCombat(combat, (Guid regionId) => WorldRepository.GetRandomNumberGenerator(regionId, 1, 6).Select(value => (UInt32)value)));
            }

            if (combatResults.Count > 0)
            {
                await WorldRepository.AddCombatResults(sessionId, combatResults);
            }

            return combatResults;
        }

        private async Task ApplyBorderClashResults(Guid sessionId, IEnumerable<CombatResult> combatResults)
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

            await WorldRepository.AddArmyToCombat(sessionId, CombatType.BorderClash, survivingArmies);
        }

        private ICommandQueue CommandQueue { get; set; }
        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
        private IWorldRepository WorldRepository { get; set; }
    }
}
