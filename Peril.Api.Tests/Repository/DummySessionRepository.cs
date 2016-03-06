using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
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

        #region - ISessionRepository Implementation -
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
        #endregion

        #region - Test Setup Helpers -
        internal DummySession SetupDummySession(Guid sessionId, String ownerId)
        {
            DummySession session = new DummySession { GameId = sessionId };
            session.SetupAddPlayer(ownerId);
            Sessions.Add(session);
            return session;
        }
        #endregion

        public List<DummySession> Sessions { get;set; }
    }

    static class ControllerMockSessionRepositoryExtensions
    {
        static public DummySession SetupDummySession(this ControllerMock controller, Guid sessionId)
        {
            return controller.SessionRepository.SetupDummySession(sessionId, controller.OwnerId);
        }

        static public DummySession SetupDummySession(this ControllerMock controller, Guid sessionId, String ownerId)
        {
            return controller.SessionRepository.SetupDummySession(sessionId, ownerId);
        }
    }
}
