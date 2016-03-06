using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/Nation")]
    public class NationController : ApiController
    {
        // GET /api/Nation/Reinforcements
        [Route("Reinforcements")]
        public async Task<UInt32> GetReinforcements()
        {
            // Check taking part in session [Forbidden]
            // Check for concurrent action [Conflict]
            // Is allowed?
            //   - Must be in correct round [ExpectationFailed]
            throw new NotImplementedException("Not implemented");
        }

        // #ToDo Cards & Missions: Stretch goal for now
    }
}
