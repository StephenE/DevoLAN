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
        public GameController(ICommandQueue commandQueue, INationRepository nationRepository, IRegionRepository regionRepository, ISessionRepository sessionRepository, IUserRepository userRepository)
        {
            CommandQueue = commandQueue;
            NationRepository = nationRepository;
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
            UserRepository = userRepository;
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
                var regions = from continentXml in worldDefinition.Root.Elements("Continent")
                              let continentId = Guid.NewGuid()
                              from regionXml in continentXml.Elements("Region")
                              select new Region
                              {
                                  RegionId = Guid.NewGuid(),
                                  ContinentId = continentId,
                                  Name = regionXml.Attribute("Name").Value
                              };

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
                    IEnumerable<ICommandQueueMessage> pendingMessages = await CommandQueue.GetQueuedCommands(session.GameId);
                    IEnumerable<IDeployReinforcementsMessage> pendingReinforcementMessages = pendingMessages.GetCommandsFromPhase(session.PhaseId)
                                                                                                            .GetQueuedDeployReinforcementsCommands();

                    IEnumerable<INationData> nations = await nationsTask;
                    await ProcessReinforcementMessages(session.GameId, await regions, nations, pendingReinforcementMessages);

                    // Reset all players to 0 reinforcements
                    Dictionary<String, UInt32> initialReinforcements = new Dictionary<string, uint>();
                    foreach (INationData nation in nations)
                    {
                        initialReinforcements[nation.UserId] = 0;
                    }
                    await NationRepository.SetAvailableReinforcements(session.GameId, initialReinforcements);
                    nextPhase = SessionPhase.CombatOrders;
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

        private ICommandQueue CommandQueue { get; set; }
        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
