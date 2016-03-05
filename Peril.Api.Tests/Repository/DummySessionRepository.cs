using Peril.Api.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummySessionRepository : ISessionRepository
    {
        public DummySessionRepository()
        {
            Sessions = new List<DummySession>();
        }

        public async Task<Guid> CreateSession()
        {
            Guid newId = Guid.NewGuid();
            Sessions.Add(new DummySession { GameId = newId });
            return newId;
        }

        public async Task<IEnumerable<IPlayer>> GetSessionPlayers(Guid sessionId)
        {
            DummySession foundSession = Sessions.Find(session => session.GameId == sessionId);
            if (foundSession != null)
            {
                return foundSession.Players;
            }
            else
            {
                return null;
            }
        }

        public async Task<IEnumerable<ISession>> GetSessions()
        {
            return Sessions;
        }

        public async Task<bool> JoinSession(Guid sessionId)
        {
            DummySession foundSession = Sessions.Find(session => session.GameId == sessionId);
            if(foundSession != null)
            {
                foundSession.Players.Add(new DummyPlayer());
            }
            return foundSession != null;
        }

        public List<DummySession> Sessions { get;set; }
    }
}
