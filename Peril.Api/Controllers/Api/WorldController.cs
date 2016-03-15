using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/World")]
    public class WorldController : ApiController
    {
        public WorldController(INationRepository nationRepository, IRegionRepository regionRepository, ISessionRepository sessionRepository)
        {
            NationRepository = nationRepository;
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
        }

        // GET /api/World/RegionList
        [Route("RegionList")]
        public async Task<IEnumerable<IRegion>> GetRegionList(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId());

            IEnumerable<IRegionData> regionData = await RegionRepository.GetRegions(session.GameId);
            return from region in regionData
                   select new Region(region);
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

        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
    }
}
