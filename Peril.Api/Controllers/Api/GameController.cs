using Peril.Api.Repository;
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
        public GameController(ISessionRepository repository)
        {
            SessionRepository = repository;
        }

        // GET /api/Game/Sessions
        [Route("Sessions")]
        public async Task<IEnumerable<Peril.Core.ISession>> GetSessions()
        {
            return await SessionRepository.GetSessions();
        }

        // POST /api/Game/StartNewGame
        [Route("StartNewGame")]
        public async Task<Peril.Core.ISession> PostStartNewSession()
        {
            Guid sessionGuid = await SessionRepository.CreateSession();
            return new Models.Session { GameId = sessionGuid };
        }

        // POST /api/Game/JoinGame?gameId=guid-as-string
        [Route("JoinGame")]
        public async Task PostJoinSession(Guid sessionId)
        {
            bool successful = await SessionRepository.JoinSession(sessionId);
            if (!successful)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
        }

        // GET /api/Game/Players
        [Route("Players")]
        public async Task<IEnumerable<Peril.Core.IPlayer>> GetPlayers(Guid sessionId)
        {
            IEnumerable<Peril.Core.IPlayer> players = await SessionRepository.GetSessionPlayers(sessionId);
            if (players == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
            return players;
        }

        private ISessionRepository SessionRepository { get; set;}
    }
}
