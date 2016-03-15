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

        public async Task<INationData> GetNation(Guid sessionId, string userId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                var query = from player in foundSession.Players
                            where player.UserId == userId
                            select player;
                return query.FirstOrDefault();
            }
            else
            {
                throw new InvalidOperationException("Called GetNation with a non-existent GUID");
            }
        }

        public async Task<IEnumerable<INationData>> GetNations(Guid sessionId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                return from player in foundSession.Players
                       select player;
            }
            else
            {
                throw new InvalidOperationException("Called GetNations with a non-existent GUID");
            }
        }

        public async Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == userId);
                if (foundPlayer != null)
                {
                    foundPlayer.CompletedPhase = phaseId;
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

        public async Task SetAvailableReinforcements(Guid sessionId, Dictionary<String, UInt32> reinforcements)
        {
            throw new NotImplementedException("Not implemented");
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
            // setupContext.ControllerMock.NationRepository.GetNation(setupContext.DummySession.GameId, userId).AvailableReinforcements = availableReinforcements;
            return setupContext;
        }
    }
}
