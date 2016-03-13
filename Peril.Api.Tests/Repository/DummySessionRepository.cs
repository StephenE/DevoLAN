using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Tests.Repository
{
    class DummySessionRepository : ISessionRepository
    {
        public DummySessionRepository()
        {
            SessionMap = new Dictionary<Guid, DummySession>();
            StorageDelaySimulationTask = Task.FromResult(false);
        }

        #region - ISessionRepository Implementation -
        public async Task<Guid> CreateSession(String userId, PlayerColour colour)
        {
            Guid newId = Guid.NewGuid();
            SessionMap[newId] = new DummySession { GameId = newId };
            await JoinSession(newId, userId, colour);
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

        public async Task<IEnumerable<ISessionData>> GetSessions()
        {
            return Sessions;
        }

        public async Task<ISessionData> GetSession(Guid sessionId)
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

        public async Task<bool> ReservePlayerColour(Guid sessionId, String sessionEtag, PlayerColour colour)
        {
            await StorageDelaySimulationTask;
            if (SessionMap.ContainsKey(sessionId))
            {
                DummySession session = SessionMap[sessionId];
                if (session.CurrentEtag == sessionEtag)
                {
                    var colourQuery = from player in session.Players
                                      where player.Colour == colour
                                      select player;
                    if(colourQuery.Count() == 0)
                    {
                        session.GenerateNewEtag();
                        return true;
                    }
                    else
                    {
                        // Already taken
                        return false;
                    }
                }
                else
                {
                    // Doesn't match!
                    throw new ConcurrencyException();
                }
            }
            else
            {
                throw new InvalidOperationException("Shouldn't call ReservePlayerColour with an invalid session GUID");
            }
        }

        public async Task JoinSession(Guid sessionId, String userId, PlayerColour colour)
        {
            DummySession foundSession = SessionMap[sessionId];
            if(foundSession != null)
            {
                foundSession.Players.Add(new DummyPlayer(userId) { Colour = colour });
                foundSession.GenerateNewEtag();
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
        internal DummySession SetupDummySession(Guid sessionId, String ownerId, PlayerColour colour)
        {
            DummySession session = new DummySession { GameId = sessionId };
            session.SetupAddPlayer(ownerId, colour);
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
        public Task StorageDelaySimulationTask { get; set; }
    }

    static class ControllerMockSessionRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupDummySession(this ControllerMock controller, Guid sessionId)
        {
            return SetupDummySession(controller, sessionId, controller.OwnerId);
        }

        static public ControllerMockSetupContext SetupDummySession(this ControllerMock controller, Guid sessionId, String ownerId)
        {
            return SetupDummySession(controller, sessionId, ownerId, PlayerColour.Black);
        }

        static public ControllerMockSetupContext SetupDummySession(this ControllerMock controller, Guid sessionId, String ownerId, PlayerColour colour)
        {
            DummySession session = controller.SessionRepository.SetupDummySession(sessionId, ownerId, colour);
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
