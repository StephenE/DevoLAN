using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ISessionRepository
    {
        Task<IEnumerable<ISession>> GetSessions();

        Task<IEnumerable<String>> GetSessionPlayers(Guid session);

        Task<Guid> CreateSession(String userId);

        Task<ISession> GetSession(Guid sessionId);

        Task JoinSession(Guid sessionId, String userId);
    }
}
