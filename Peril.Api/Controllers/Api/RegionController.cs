using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Controllers.Api
{
    [Authorize]
    [RoutePrefix("api/Region")]
    public class RegionController : ApiController
    {
        public RegionController(ICommandQueue commandQueue, INationRepository nationRepository, IRegionRepository regionRepository, ISessionRepository sessionRepository, IUserRepository userRepository)
        {
            CommandQueue = commandQueue;
            NationRepository = nationRepository;
            RegionRepository = regionRepository;
            SessionRepository = sessionRepository;
            UserRepository = userRepository;
        }

        // GET /api/Region/Details
        [Route("Details")]
        public async Task<IRegion> GetDetails(Guid regionId)
        {
            IRegionData region = await RegionRepository.GetRegionOrThrow(regionId);
            ISession session = await SessionRepository.GetSessionOrThrow(region)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId());

            return new Region(region);
        }

        // POST /api/Region/Deploy
        [Route("Deploy")]
        public async Task<Guid> PostDeployTroops(Guid regionId, uint numberOfTroops)
        {
            IRegionData region = await RegionRepository.GetRegionOrThrow(regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(region)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Reinforcements);
            INationData nation = await NationRepository.GetNation(User.Identity.GetUserId());

            if(nation.AvailableReinforcements < numberOfTroops)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to deploy" });
            }
            else
            {
                return await CommandQueue.DeployReinforcements(session.PhaseId, nation.CurrentEtag, region.RegionId, numberOfTroops);
            }
        }

        // POST /api/Region/Attack
        [Route("Attack")]
        public async Task<Guid> PostAttack(Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            IRegionData sourceRegion = await RegionRepository.GetRegionOrThrow(regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(sourceRegion)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.CombatOrders);
            IRegionData targetRegion = await RegionRepository.GetRegionOrThrow(targetRegionId)
                                                             .IsNotRegionOwnerOrThrow(User.Identity.GetUserId())
                                                             .IsRegionConnectedOrThrow(sourceRegion.RegionId);

            if (sourceRegion.TroopCount <= (sourceRegion.TroopsCommittedToPhase + numberOfTroops))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to attack with" });
            }
            else
            {
                return await CommandQueue.OrderAttack(session.PhaseId, sourceRegion.RegionId, sourceRegion.CurrentEtag, targetRegion.RegionId, numberOfTroops);
            }
        }

        // POST /api/Region/Redeploy
        [Route("Redeploy")]
        public async Task<Guid> PostRedeployTroops(Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            IRegionData sourceRegion = await RegionRepository.GetRegionOrThrow(regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(sourceRegion)
                                                      .IsUserIdJoinedOrThrow(SessionRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Redeployment);
            IRegionData targetRegion = await RegionRepository.GetRegionOrThrow(targetRegionId)
                                                             .IsRegionOwnerOrThrow(User.Identity.GetUserId())
                                                             .IsRegionConnectedOrThrow(sourceRegion.RegionId);
            INationData nation = await NationRepository.GetNation(User.Identity.GetUserId());

            if (sourceRegion.TroopCount <= numberOfTroops)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to redeploy" });
            }
            else
            {
                return await CommandQueue.Redeploy(session.PhaseId, nation.CurrentEtag, sourceRegion.RegionId, targetRegion.RegionId, numberOfTroops);
            }
        }

        private ICommandQueue CommandQueue { get; set; }
        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
