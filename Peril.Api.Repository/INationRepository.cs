using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface INationRepository
    {
        Task<INationData> GetNation(Guid sessionId, String userId);

        Task<IEnumerable<INationData>> GetNations(Guid sessionId);

        Task<IEnumerable<ICardData>> GetCards(Guid sessionId, String userId);

        Task<IEnumerable<ICardData>> GetUnownedCards(Guid sessionId);

        void SetCardOwner(IBatchOperationHandle batchOperation, Guid sessionId, Guid regionId, String userId, String currentEtag);

        void SetCardDiscarded(IBatchOperationHandle batchOperation, Guid sessionId, Guid regionId, String currentEtag);

        Task ResetDiscardedCards(IBatchOperationHandle batchOperation, Guid sessionId);

        void SetAvailableReinforcements(IBatchOperationHandle batchOperation, Guid sessionId, String userId, String currentEtag, UInt32 reinforcements);

        Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId);
    }
}
