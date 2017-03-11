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
        public WorldController(INationRepository nationRepository, IRegionRepository regionRepository, ISessionRepository sessionRepository, IWorldRepository worldRepository)
        {
            NationRepository = nationRepository;
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
            WorldRepository = worldRepository;
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
        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId());

            IEnumerable<ICombat> combatData = await WorldRepository.GetCombat(session.GameId, session.Round);
            return from combat in combatData
                   where IsStillValid(session, combat)
                   select new Combat(combat);
        }

        // POST /api/World/Combat
        [Route("CombatResult")]
        public Task<ICombatResult> GetCombatResult(Guid combatId)
        {
            // Check taking part in session [Forbidden]
            // Is allowed?
            //   - Must be valid combat id
            //   - Must be in correct round
            throw new NotImplementedException("Not implemented");
        }

        private bool IsStillValid(ISession session, ICombat combat)
        {
            switch(session.PhaseType)
            {
                case SessionPhase.BorderClashes:
                    return true;
                case SessionPhase.MassInvasions:
                    return combat.ResolutionType >= CombatType.MassInvasion;
                case SessionPhase.Invasions:
                    return combat.ResolutionType >= CombatType.Invasion;
                case SessionPhase.SpoilsOfWar:
                    return combat.ResolutionType >= CombatType.SpoilsOfWar;
                default:
                    return false;
            }
        }

        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IWorldRepository WorldRepository { get; set; }
    }
}
