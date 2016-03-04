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
        // GET /api/Game/Sessions
        [Route("Sessions")]
        public async Task<IEnumerable<Peril.Core.ISession>> GetSessions()
        {
            return null;
        }

        // POST /api/Game/StartNewGame
        [Route("StartNewGame")]
        public async Task<Peril.Core.ISession> PostStartNewSession()
        {
            return null;
        }

        // POST /api/Game/JoinGame?gameId=guid-as-string
        [Route("JoinGame")]
        public async Task PostJoinSession(Guid sessionId)
        {
            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
        }

        // GET /api/Game/Players
        [Route("Players")]
        public async Task<IEnumerable<Peril.Core.IPlayer>> GetPlayers(Guid sessionId)
        {
            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
        }
    }
}
