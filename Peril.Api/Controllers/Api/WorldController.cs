using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
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
        public WorldController(IRegionRepository regionRepository, ISessionRepository sessionRepository)
        {
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
        }

        // GET /api/World/Regions
        [Route("Regions")]
        public async Task<IEnumerable<IRegion>> GetRegions(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId());

            return await RegionRepository.GetRegions(session.GameId);
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

        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
    }
}
