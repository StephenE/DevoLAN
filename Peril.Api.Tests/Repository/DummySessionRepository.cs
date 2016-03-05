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

        public async Task<Guid> CreateSession(String userId)
        {
            Guid newId = Guid.NewGuid();
            Sessions.Add(new DummySession { GameId = newId });
            await JoinSession(newId, userId);
            return newId;
        }

        public async Task<IEnumerable<String>> GetSessionPlayers(Guid sessionId)
        {
            DummySession foundSession = Sessions.Find(session => session.GameId == sessionId);
            if (foundSession != null)
            {
                return foundSession.Players;
            }
            else
            {
                throw new InvalidOperationException("Called GetSessionPlayers with a non-existant GUID");
            }
        }

        public async Task<IEnumerable<ISession>> GetSessions()
        {
            return Sessions;
        }

        public async Task<ISession> GetSession(Guid sessionId)
        {
            return Sessions.Find(session => session.GameId == sessionId);
        }

        public async Task JoinSession(Guid sessionId, String userId)
        {
            DummySession foundSession = Sessions.Find(session => session.GameId == sessionId);
            if(foundSession != null)
            {
                foundSession.Players.Add(userId);
            }
            else
            {
                throw new InvalidOperationException("Called JoinSession with a non-existant GUID");
            }
        }

        public List<DummySession> Sessions { get;set; }
    }
}
