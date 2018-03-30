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

        public void SetAvailableReinforcements(IBatchOperationHandle batchOperationHandle, Guid sessionId, Dictionary<String, UInt32> reinforcements)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            DummyBatchOperationHandle batchOperation = batchOperationHandle as DummyBatchOperationHandle;
            if (foundSession != null)
            {
                foreach (var playerEntry in reinforcements)
                {
                    DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == playerEntry.Key);
                    if (foundPlayer != null)
                    {
                        batchOperation.QueuedOperations.Add(() =>
                        {
                            foundPlayer.AvailableReinforcements = playerEntry.Value;
                        });
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
            using (DummyBatchOperationHandle batchOperation = new DummyBatchOperationHandle())
            {
                setupContext.ControllerMock.NationRepository.SetAvailableReinforcements(batchOperation, setupContext.DummySession.GameId, new Dictionary<String, UInt32>() { { userId, availableReinforcements } });
            }
            return setupContext;
        }

        static public ControllerMockSetupContext SetupCardOwner(this ControllerMockSetupContext setupContext, Guid regionId)
        {
            return SetupCardOwner(setupContext, setupContext.ControllerMock.OwnerId, regionId);
        }

        static public ControllerMockSetupContext SetupCardOwner(this ControllerMockSetupContext setupContext, String userId, Guid regionId)
        {
            using (DummyBatchOperationHandle batchOperation = new DummyBatchOperationHandle())
            {
                // setupContext.ControllerMock.NationRepository.SetAvailableReinforcements(batchOperation, setupContext.DummySession.GameId, new Dictionary<String, UInt32>() { { userId, availableReinforcements } });
            }
            return setupContext;
        }

        static public ControllerMockSetupContext SetupPlayerCompletedPhase(this ControllerMockSetupContext setupContext)
        {
            return SetupPlayerCompletedPhase(setupContext, setupContext.ControllerMock.OwnerId, setupContext.DummySession.PhaseId);
        }

        static public ControllerMockSetupContext SetupPlayerCompletedPhase(this ControllerMockSetupContext setupContext, String userId)
        {
            return SetupPlayerCompletedPhase(setupContext, userId, setupContext.DummySession.PhaseId);
        }

        static public ControllerMockSetupContext SetupPlayerCompletedPhase(this ControllerMockSetupContext setupContext, String userId, Guid phase)
        {
            var nationData = setupContext.ControllerMock.GetNation(setupContext.DummySession.GameId, userId);
            nationData.CompletedPhase = phase;
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
