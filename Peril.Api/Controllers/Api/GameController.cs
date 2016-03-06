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

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/Game")]
    public class GameController : ApiController
    {
        public GameController(ISessionRepository sessionRepository, IUserRepository userRepository)
        {
            SessionRepository = sessionRepository;
            UserRepository = userRepository;
        }

        // GET /api/Game/Sessions
        [Route("Sessions")]
        public async Task<IEnumerable<Peril.Core.ISession>> GetSessions()
        {
            return await SessionRepository.GetSessions();
        }

        // GET /api/Game/Session
        [Route("Session")]
        public async Task<Peril.Core.ISession> GetSession(Guid sessionId)
        {
            return await SessionRepository.GetSessionOrThrow(sessionId);
        }

        // POST /api/Game/StartNewGame
        [Route("StartNewGame")]
        public async Task<Peril.Core.ISession> PostStartNewSession()
        {
            Guid sessionGuid = await SessionRepository.CreateSession(User.Identity.GetUserId());
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
        public async Task<IEnumerable<Peril.Core.IPlayer>> GetPlayers(Guid sessionId)
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

        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
