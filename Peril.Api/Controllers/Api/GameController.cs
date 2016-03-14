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
        public GameController(IRegionRepository regionRepository, ISessionRepository sessionRepository, IUserRepository userRepository)
        {
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
            catch(Exception error)
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

            IEnumerable<IPlayer> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
            var existingEntry = playerIds.Where(playerId => playerId.UserId == User.Identity.GetUserId());
            if (existingEntry.Count() == 0)
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
            IEnumerable<IPlayer> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
            return from player in playerIds
                    join user in UserRepository.Users on player.UserId equals user.Id
                    select new Player { UserId = player.UserId, Name = user.UserName, Colour = player.Colour };
        }

        // POST /api/Game/PostEndPhase
        [Route("EndPhase")]
        public async Task PostEndPhase(Guid sessionId, Guid phaseId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseIdOrThrow(phaseId)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId());

            await SessionRepository.MarkPlayerCompletedPhase(sessionId, User.Identity.GetUserId(), phaseId);
        }

        // POST /api/Game/PostAdvanceNextPhase
        [Route("AdvanceNextPhase")]
        public async Task PostAdvanceNextPhase(Guid sessionId, Guid phaseId, bool force)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseIdOrThrow(phaseId)
                                                      .IsSessionOwnerOrThrow(User.Identity.GetUserId());

            switch(session.PhaseType)
            {
                case SessionPhase.NotStarted:
                {
                    IEnumerable<IRegionData> regions = await RegionRepository.GetRegions(session.GameId);
                    IEnumerable<IPlayer> players = await SessionRepository.GetSessionPlayers(session.GameId);
                    await DistributeInitialRegions(regions, players);
                    break;
                }
                default:
                {
                    throw new NotImplementedException("Not done yet");
                }
            }
            
            // Check all players ready (unless force == true)
        }

        private async Task DistributeInitialRegions(IEnumerable<IRegionData> availableRegions, IEnumerable<IPlayer> availablePlayers)
        {
            Dictionary<Guid, String> assignedRegions = new Dictionary<Guid, String>();

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
            List<IPlayer> playersToBeAssigned = availablePlayers.ToList();
            playersToBeAssigned.Shuffle(random);
            int regionIndex = 0;
            for(int counter = 0; counter < extraRegions; ++counter, ++regionIndex)
            {
                IRegionData region = regionsToBeAssigned[regionIndex];
                IPlayer player = playersToBeAssigned[counter];
                assignedRegions[region.RegionId] = player.UserId;
            }

            // Distribute the remaining regions in a round robin fashion
            for (int roundCounter = 0; roundCounter < minimumNumberOfRegionsPerPlayer; ++roundCounter)
            {
                playersToBeAssigned.Shuffle(random);
                for (int playerCounter = 0; playerCounter < playersToBeAssigned.Count; ++playerCounter, ++regionIndex)
                {
                    IRegionData region = regionsToBeAssigned[regionIndex];
                    IPlayer player = playersToBeAssigned[playerCounter];
                    assignedRegions[region.RegionId] = player.UserId;
                }
            }
        }

        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
