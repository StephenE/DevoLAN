using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    

    public interface IRegionRepository
    {
        String WorldDefinitionPath { get; }

        Task CreateRegion(Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions);

        Task<IRegionData> GetRegion(Guid sessionId, Guid regionId);

        Task<IEnumerable<IRegionData>> GetRegions(Guid sessionId);

        void AssignRegionOwnership(IBatchOperationHandle batchOperationHandle, Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges);
    }
}
