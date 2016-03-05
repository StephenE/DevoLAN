using Peril.Api.Models;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/Region")]
    public class RegionController : ApiController
    {
        // GET /api/Region/Details
        [Route("Details")]
        public async Task<IRegion> GetDetails(Guid regionId)
        {
            throw new NotImplementedException("Not implemented");
        }

        // POST /api/Region/Deploy
        [Route("Deploy")]
        public async Task PostDeployTroops(Guid regionId, uint numberOfTroops)
        {
            // Check for concurrent action [Conflict]
            // Is allowed?
            //   - Must be valid region [NotFound]
            //   - Must be in correct round [ExpectationFailed]
            //   - Must own region [PreconditionFailed]
            //   - Must have the specified number of troops [BadRequest]
            // Add troops to region & decrease available

            throw new NotImplementedException("Not implemented");
        }

        // POST /api/Region/Attack
        [Route("Attack")]
        public async Task PostAttack(Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            // Check for concurrent action [Conflict]
            // Is allowed?
            //   - Must be valid region [NotFound]
            //   - Must be in correct round [ExpectationFailed]
            //   - Must own region [PreconditionFailed]
            //   - Must not own target region [NotAcceptable]
            //   - Must have enough troops (not already commited) [BadRequest]
            //   - Target region must be connected [PaymentRequired]
            // Queue attack

            throw new NotImplementedException("Not implemented");
        }

        // GET /api/Region/Attack
        [Route("Attack")]
        public async Task<IEnumerable<AttackDetails>> GetAttack(Guid regionId)
        {
            // Is allowed?
            //   - Must be valid region [NotFound]
            //   - Must be in correct round [ExpectationFailed]
            //   - Must own region [PreconditionFailed]
            // Return List<Attacks>

            throw new NotImplementedException("Not implemented");
        }

        // DELETE /api/Region/Attack
        [Route("Attack")]
        public async Task DeleteAttack(Guid regionId, Guid targetRegionId)
        {
            // Check for concurrent action [Conflict]
            // Is allowed?
            //   - Must be valid region [NotFound]
            //   - Must be in correct round [ExpectationFailed]
            //   - Must own region [PreconditionFailed]
            // Unqueue attack (if any)

            throw new NotImplementedException("Not implemented");
        }

        // POST /api/Region/Redeploy
        [Route("Redeploy")]
        public async Task PostRedeployTroops(Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            // Is allowed?
            //   - Must be valid region [NotFound]
            //   - Must be in correct round
            //   - Must own both region
            //   - Must have the specified number of troops
            //   - Target region must be connected
            // Add troops to target region & remove troops from source region

            throw new NotImplementedException("Not implemented");
        }
    }
}
