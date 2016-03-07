using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ICommandQueue
    {
        Task<Guid> DeployReinforcements(Guid phaseId, String nationEtag, Guid targetRegion, UInt32 numberOfTroops);

        Task<Guid> OrderAttack(Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops);

        Task<Guid> Redeploy(Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops);
    }
}
