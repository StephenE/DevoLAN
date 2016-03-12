using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
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
        public async Task<ISession> PostStartNewSession()
        {
            Guid sessionGuid = await SessionRepository.CreateSession(User.Identity.GetUserId());

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
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, ReasonPhrase = error.Message });
            }

            return new Models.Session { GameId = sessionGuid };
        }

        // POST /api/Game/JoinGame?gameId=guid-as-string
        [Route("JoinGame")]
        public async Task PostJoinSession(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseTypeOrThrow(SessionPhase.NotStarted);

            IEnumerable<String> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
            var existingEntry = playerIds.Where(playerId => playerId == User.Identity.GetUserId());
            if (existingEntry.Count() == 0)
            {
                await SessionRepository.JoinSession(sessionId, User.Identity.GetUserId());
            }
        }

        // GET /api/Game/Players
        [Route("Players")]
        public async Task<IEnumerable<IPlayer>> GetPlayers(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId);

            // Resolve player ids into IPlayer structures
            IEnumerable<String> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
            return from playerId in playerIds
                    join user in UserRepository.Users on playerId equals user.Id
                    select new Player { UserId = playerId, Name = user.UserName };
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
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId());

            // Check for concurrent action [Conflict]
            // Only allowed by session owner [Forbidden]
            // Check all players ready (unless force == true)
            throw new NotImplementedException("Not done yet");
        }

        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
