using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummyNationRepository : INationRepository
    {
        public DummyNationRepository(DummySessionRepository sessionRepository)
        {
            SessionRepository = sessionRepository;
        }

        public Task<INationData> GetNation(Guid sessionId, string userId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                var query = from player in foundSession.Players
                            where player.UserId == userId
                            select player;
                return Task.FromResult<INationData>(query.FirstOrDefault());
            }
            else
            {
                throw new InvalidOperationException("Called GetNation with a non-existent GUID");
            }
        }

        public Task<IEnumerable<INationData>> GetNations(Guid sessionId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                IEnumerable<INationData> results = from player in foundSession.Players
                                                    select player;
                return Task.FromResult(results);
            }
            else
            {
                throw new InvalidOperationException("Called GetNations with a non-existent GUID");
            }
        }

        public Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == userId);
                if (foundPlayer != null)
                {
                    foundPlayer.CompletedPhase = phaseId;
                    return Task.FromResult(false);
                }
                else
                {
                    throw new InvalidOperationException("Called MarkPlayerCompletedPhase with a non-existent user id");
                }
            }
            else
            {
                throw new InvalidOperationException("Called JoinSession with a non-existent GUID");
            }
        }

        public Task SetAvailableReinforcements(Guid sessionId, Dictionary<String, UInt32> reinforcements)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                foreach (var playerEntry in reinforcements)
                {
                    DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == playerEntry.Key);
                    if (foundPlayer != null)
                    {
                        foundPlayer.AvailableReinforcements = playerEntry.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException("Called MarkPlayerCompletedPhase with a non-existent user id");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Called JoinSession with a non-existent GUID");
            }

            return Task.FromResult(false);
        }

        private DummySessionRepository SessionRepository;
    }

    static class ControllerMockNationRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupAvailableReinforcements(this ControllerMockSetupContext setupContext, UInt32 availableReinforcements)
        {
            return SetupAvailableReinforcements(setupContext, setupContext.ControllerMock.OwnerId, availableReinforcements);
        }

        static public ControllerMockSetupContext SetupAvailableReinforcements(this ControllerMockSetupContext setupContext, String userId, UInt32 availableReinforcements)
        {
            var task = setupContext.ControllerMock.NationRepository.SetAvailableReinforcements(setupContext.DummySession.GameId, new Dictionary<String, UInt32>() { { userId, availableReinforcements } });
            task.Wait();
            return setupContext;
        }

        static public DummyNationData GetNation(this ControllerMock controllerMock, Guid sessionId, String userId)
        {
            var task = controllerMock.NationRepository.GetNation(sessionId, userId);
            task.Wait();
            return task.Result as DummyNationData;
        }
    }
}
