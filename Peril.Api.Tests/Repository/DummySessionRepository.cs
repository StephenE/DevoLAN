using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummySessionRepository : ISessionRepository
    {
        public DummySessionRepository()
        {
            SessionMap = new Dictionary<Guid, DummySession>();
        }

        #region - ISessionRepository Implementation -
        public async Task<Guid> CreateSession(String userId)
        {
            Guid newId = Guid.NewGuid();
            SessionMap[newId] = new DummySession { GameId = newId };
            await JoinSession(newId, userId);
            return newId;
        }

        public async Task<IEnumerable<IPlayer>> GetSessionPlayers(Guid sessionId)
        {
            DummySession foundSession = SessionMap[sessionId];
            if (foundSession != null)
            {
                return from player in foundSession.Players
                       select player;
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
            if(SessionMap.ContainsKey(sessionId))
            {
                return SessionMap[sessionId];
            }
            else
            {
                return null;
            }
        }

        public async Task JoinSession(Guid sessionId, String userId)
        {
            DummySession foundSession = SessionMap[sessionId];
            if(foundSession != null)
            {
                foundSession.Players.Add(new DummyPlayer(userId));
            }
            else
            {
                throw new InvalidOperationException("Called JoinSession with a non-existant GUID");
            }
        }

        public async Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId)
        {
            DummySession foundSession = SessionMap[sessionId];
            if (foundSession != null)
            {
                DummyPlayer foundPlayer = foundSession.Players.Find(player => player.UserId == userId);
                if(foundPlayer != null)
                {
                    foundPlayer.CompletedPhase = phaseId;
                }
                else
                {
                    throw new InvalidOperationException("Called MarkPlayerCompletedPhase with a non-existant user id");
                }
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
            SessionMap[sessionId] = session;
            return session;
        }
        #endregion

        public IEnumerable<DummySession> Sessions
        {
            get
            {
                return from session in SessionMap
                       select session.Value;
            }
        }

        public Dictionary<Guid, DummySession> SessionMap { get; set; }
    }

    static class ControllerMockSessionRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupDummySession(this ControllerMock controller, Guid sessionId)
        {
            return SetupDummySession(controller, sessionId, controller.OwnerId);
        }

        static public ControllerMockSetupContext SetupDummySession(this ControllerMock controller, Guid sessionId, String ownerId)
        {
            DummySession session = controller.SessionRepository.SetupDummySession(sessionId, ownerId);
            controller.NationRepository.SetupDummyNation(session.GameId, ownerId);
            return new ControllerMockSetupContext { ControllerMock = controller, DummySession = session };
        }

        static public ControllerMockSetupContext SetupSessionPhase(this ControllerMockSetupContext setupContext, SessionPhase phase)
        {
            setupContext.DummySession.SetupSessionPhase(phase);
            return setupContext;
        }
    }
}
