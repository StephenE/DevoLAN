using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/World")]
    public class WorldController : ApiController
    {
        // GET /api/World/Regions
        [Route("Regions")]
        public async Task<IEnumerable<IRegion>> GetRegions()
        {
            // Check taking part in session [Forbidden]
            throw new NotImplementedException("Not implemented");
        }

        // GET /api/World/Combat
        [Route("Combat")]
        public async Task<IEnumerable<ICombat>> GetCombat()
        {
            // Check taking part in session [Forbidden]
            // Is allowed?
            //   - Must be in correct round
            throw new NotImplementedException("Not implemented");
        }

        // POST /api/World/Combat
        [Route("CombatResult")]
        public async Task<ICombatResult> GetCombatResult(Guid combatId)
        {
            // Check taking part in session [Forbidden]
            // Is allowed?
            //   - Must be valid combat id
            //   - Must be in correct round
            throw new NotImplementedException("Not implemented");
        }
    }
}
