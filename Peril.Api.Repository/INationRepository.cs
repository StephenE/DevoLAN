using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface INationRepository
    {
        Task<INationData> GetNation(Guid sessionId, String userId);

        Task<IEnumerable<INationData>> GetNations(Guid sessionId);

        Task SetAvailableReinforcements(Guid sessionId, Dictionary<String, UInt32> reinforcements);

        Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId);
    }
}
