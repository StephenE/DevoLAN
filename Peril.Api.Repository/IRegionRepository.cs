using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{


    public interface IRegionRepository
    {
        String WorldDefinitionPath { get; }

        void CreateRegion(IBatchOperationHandle batchOperationHandle, Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions, UInt32 cardValue);

        Task<IRegionData> GetRegion(Guid sessionId, Guid regionId);

        Task<IEnumerable<IRegionData>> GetRegions(Guid sessionId);

        void AssignRegionOwnership(IBatchOperationHandle batchOperationHandle, Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges);

        void AssignRegionOwnership(IBatchOperationHandle batchOperationHandle, IEnumerable<IRegionData> regions, Dictionary<Guid, OwnershipChange> ownershipChanges);

        void CommitTroopsToPhase(IBatchOperationHandle batchOperationHandle, IRegionData sourceRegion, UInt32 troopsToCommit);
    }
}
