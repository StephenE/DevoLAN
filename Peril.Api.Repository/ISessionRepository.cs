using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ISessionRepository
    {
        Task<IEnumerable<ISession>> GetSessions();

        Task<IEnumerable<IPlayer>> GetSessionPlayers(Guid session);

        Task<Guid> CreateSession();

        Task<bool> JoinSession(Guid sessionId);
    }
}
