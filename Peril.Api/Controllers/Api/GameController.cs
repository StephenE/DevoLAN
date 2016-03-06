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
            return await SessionRepository.GetSession(sessionId);
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
            ISession session = await SessionRepository.GetSession(sessionId);
            if (session == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
            else if(session.PhaseType != SessionPhase.NotStarted)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = "The session is no longer in the 'NotStarted' state" });
            }
            else
            {
                IEnumerable<String> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
                var existingEntry = playerIds.Where(playerId => playerId == User.Identity.GetUserId());
                if (existingEntry.Count() == 0)
                {
                    await SessionRepository.JoinSession(sessionId, User.Identity.GetUserId());
                }
            }
        }

        // GET /api/Game/Players
        [Route("Players")]
        public async Task<IEnumerable<Peril.Core.IPlayer>> GetPlayers(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSession(sessionId);
            if (session == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
            else
            {
                // Resolve player ids into IPlayer structures
                IEnumerable<String> playerIds = await SessionRepository.GetSessionPlayers(sessionId);
                return from playerId in playerIds
                       join user in UserRepository.Users on playerId equals user.Id
                       select new Player { UserId = playerId, Name = user.UserName };
            }
        }

        // POST /api/Game/PostEndPhase
        [Route("EndPhase")]
        public async Task PostEndPhase(Guid roundId)
        {
            // Check for concurrent action [Conflict]
            throw new NotImplementedException("Not done yet");
        }

        // POST /api/Game/PostAdvanceNextPhase
        [Route("AdvanceNextPhase")]
        public async Task PostAdvanceNextPhase(Guid roundId, bool force)
        {
            // Check for concurrent action [Conflict]
            // Only allowed by session owner [Forbidden]
            // Check all players ready (unless force == true)
            throw new NotImplementedException("Not done yet");
        }

        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
