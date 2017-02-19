using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ISessionRepository
    {
        Task<IEnumerable<ISessionData>> GetSessions();

        Task<Guid> CreateSession(String userId, PlayerColour colour);

        Task<ISessionData> GetSession(Guid sessionId);

        IBatchOperationHandle StartBatchOperation(Guid sessionId);

        Task<bool> ReservePlayerColour(Guid sessionId, String sessionEtag, PlayerColour colour);

        Task JoinSession(Guid sessionId, String userId, PlayerColour colour);

        Task SetSessionPhase(Guid sessionId, Guid currentPhaseId, SessionPhase newPhase);
    }
}
