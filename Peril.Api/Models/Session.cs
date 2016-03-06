using Peril.Api.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Models
{
    public class Session : ISession
    {
        public Guid GameId { get; set; }
        public Guid PhaseId { get; set; }
        public SessionPhase PhaseType { get; set; }
    }

    static public class SessionRepositoryExtensionMethods
    {
        static public async Task<ISession> GetSessionOrThrow(this ISessionRepository repository, Guid sessionId)
        {
            ISession session = await repository.GetSession(sessionId);
            if (session == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
            return session;
        }

        static public async Task<ISession> IsPhaseIdOrThrow(this Task<ISession> sessionTask, Guid phaseId)
        {
            ISession session = await sessionTask;
            if (session.PhaseId != phaseId)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = "The session is no longer in the specified phase" });
            }
            return session;
        }

        static public async Task<ISession> IsPhaseTypeOrThrow(this Task<ISession> sessionTask, SessionPhase phaseType)
        {
            ISession session = await sessionTask;
            if (session.PhaseType != phaseType)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = String.Format("The session is no longer in the {0} phase", phaseType) });
            }
            return session;
        }

        static public async Task<ISession> IsUserIdJoinedOrThrow(this Task<ISession> sessionTask, ISessionRepository repository, String userId)
        {
            ISession session = await sessionTask;
            IEnumerable<String> playersInSession = await repository.GetSessionPlayers(session.GameId);
            if (!playersInSession.Contains(userId))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "You have not joined the specified session" });
            }
            return session;
        }
    }
}