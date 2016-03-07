using Peril.Api.Repository;
using System;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummyCommandQueue : ICommandQueue
    {
        public Task<Guid> DeployReinforcements(Guid phaseId, string nationEtag, Guid targetRegion, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> OrderAttack(Guid phaseId, Guid sourceRegion, string sourceRegionEtag, Guid targetRegion, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> Redeploy(Guid phaseId, string nationEtag, Guid sourceRegion, Guid targetRegion, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }
    }
}
