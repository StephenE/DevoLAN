﻿using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Api.Tests.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummyNationRepository : INationRepository
    {
        public DummyNationRepository(DummySessionRepository sessionRepository, DummyRegionRepository regionRepository)
        {
            SessionRepository = sessionRepository;
            RegionRepository = regionRepository;
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

        public Task<IEnumerable<ICardData>> GetCards(Guid sessionId, String userId)
        {
            IEnumerable<DummyCardData> cards = RegionRepository.CardData.Where(card => card.Value.OwnerId == userId).Select(card => card.Value);
            return Task.FromResult(cards.Cast<ICardData>());
        }

        public Task<IEnumerable<ICardData>> GetUnownedCards(Guid sessionId)
        {
            IEnumerable<DummyCardData> cards = RegionRepository.CardData.Where(card => card.Value.OwnerId == DummyCardData.UnownedCard).Select(card => card.Value);
            return Task.FromResult(cards.Cast<ICardData>());
        }

        public void SetCardOwner(IBatchOperationHandle batchOperationHandle, Guid sessionId, Guid regionId, String userId, String currentEtag)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == userId);
                if (foundPlayer != null)
                {
                    SetCardOwnerInternal(batchOperationHandle, sessionId, regionId, userId, currentEtag);
                }
                else
                {
                    throw new InvalidOperationException("Called SetCardOwner with a non-existent user id");
                }
            }
            else
            {
                throw new InvalidOperationException("Called SetCardOwner with a non-existent session id");
            }
        }

        public void SetCardOwnerInternal(IBatchOperationHandle batchOperationHandle, Guid sessionId, Guid regionId, String userId, String currentEtag)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            if (foundSession != null)
            {
                if (RegionRepository.CardData.ContainsKey(regionId))
                {
                    DummyBatchOperationHandle batchOperation = batchOperationHandle as DummyBatchOperationHandle;
                    batchOperation.QueuedOperations.Add(() =>
                    {
                        RegionRepository.CardData[regionId].OwnerId = userId;
                    });
                }
                else
                {
                    throw new InvalidOperationException("Called SetCardOwner with a non-existent region id");
                }
            }
            else
            {
                throw new InvalidOperationException("Called SetCardOwner with a non-existent session id");
            }
        }

        public void SetCardDiscarded(IBatchOperationHandle batchOperation, Guid sessionId, Guid regionId, string currentEtag)
        {
            SetCardOwnerInternal(batchOperation, sessionId, regionId, DummyCardData.UsedCard, currentEtag);
        }

        public Task ResetDiscardedCards(IBatchOperationHandle batchOperationHandle, Guid sessionId)
        {
            DummyBatchOperationHandle batchOperation = batchOperationHandle as DummyBatchOperationHandle;
            List<ICardData> results = new List<ICardData>();
            foreach(DummyCardData card in RegionRepository.CardData.Values)
            {
                if (card.OwnerId == DummyCardData.UsedCard)
                {
                    batchOperation.QueuedOperations.Add(() =>
                    {
                        card.OwnerId = DummyCardData.UnownedCard;
                    });
                    results.Add(card);
                }
            }
            return Task.FromResult(0);
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

        public void SetAvailableReinforcements(IBatchOperationHandle batchOperationHandle, Guid sessionId, string userId, string currentEtag, uint reinforcements)
        {
            DummySession foundSession = SessionRepository.SessionMap[sessionId];
            DummyBatchOperationHandle batchOperation = batchOperationHandle as DummyBatchOperationHandle;
            if (foundSession != null)
            {
                DummyNationData foundPlayer = foundSession.Players.Find(player => player.UserId == userId);
                if (foundPlayer != null)
                {
                    batchOperation.QueuedOperations.Add(() =>
                    {
                        foundPlayer.AvailableReinforcements = reinforcements;
                    });
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

        private DummySessionRepository SessionRepository;
        private DummyRegionRepository RegionRepository;
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
                setupContext.ControllerMock.NationRepository.SetAvailableReinforcements(batchOperation, setupContext.DummySession.GameId, userId, String.Empty, availableReinforcements);
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
                setupContext.ControllerMock.NationRepository.SetCardOwner(batchOperation, setupContext.DummySession.GameId, regionId, userId, String.Empty);
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
