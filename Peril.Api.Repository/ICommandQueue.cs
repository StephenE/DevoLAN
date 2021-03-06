﻿using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ICommandQueue
    {
        Task<Guid> DeployReinforcements(Guid sessionId, Guid phaseId, Guid targetRegion, String targetRegionEtag, UInt32 numberOfTroops);

        Task<Guid> OrderAttack(IBatchOperationHandle batchOperationHandle, Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops);

        Task<Guid> Redeploy(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops);

        Task<IEnumerable<ICommandQueueMessage>> GetQueuedCommands(Guid sessionId, Guid sessionPhaseId);

        void RemoveCommand(IBatchOperationHandle batchOperation, ICommandQueueMessage command);

        void RemoveCommands(IBatchOperationHandle batchOperation, IEnumerable<ICommandQueueMessage> commands);
    }
}
