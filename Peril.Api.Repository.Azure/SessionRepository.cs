using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Core;

namespace Peril.Api.Repository.Azure
{
    class SessionRepository : ISessionRepository
    {
        public Task<Guid> CreateSession()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IPlayer>> GetSessionPlayers(Guid session)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ISession>> GetSessions()
        {
            throw new NotImplementedException();
        }

        public Task<bool> JoinSession(Guid sessionId)
        {
            throw new NotImplementedException();
        }
    }
}
