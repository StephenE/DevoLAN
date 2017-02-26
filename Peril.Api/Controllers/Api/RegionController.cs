﻿using Microsoft.AspNet.Identity;
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
        public async Task<IRegion> GetDetails(Guid sessionId, Guid regionId)
        {
            IRegionData region = await RegionRepository.GetRegionOrThrow(sessionId, regionId);
            ISession session = await SessionRepository.GetSessionOrThrow(region)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId());

            return new Region(region);
        }

        // POST /api/Region/Deploy
        [Route("Deploy")]
        public async Task<Guid> PostDeployTroops(Guid sessionId, Guid regionId, uint numberOfTroops)
        {
            IRegionData region = await RegionRepository.GetRegionOrThrow(sessionId, regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(region)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Reinforcements);
            INationData nation = await NationRepository.GetNation(sessionId, User.Identity.GetUserId());

            if(nation.AvailableReinforcements < numberOfTroops)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to deploy" });
            }
            else
            {
                return await CommandQueue.DeployReinforcements(session.GameId, session.PhaseId, region.RegionId, region.CurrentEtag, numberOfTroops);
            }
        }

        // POST /api/Region/Attack
        [Route("Attack")]
        public async Task<Guid> PostAttack(Guid sessionId, Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            IRegionData sourceRegion = await RegionRepository.GetRegionOrThrow(sessionId, regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(sourceRegion)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.CombatOrders);
            IRegionData targetRegion = await RegionRepository.GetRegionOrThrow(session.GameId, targetRegionId)
                                                             .IsNotRegionOwnerOrThrow(User.Identity.GetUserId())
                                                             .IsRegionConnectedOrThrow(sourceRegion.RegionId);

            if (sourceRegion.TroopCount <= (sourceRegion.TroopsCommittedToPhase + numberOfTroops))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to attack with" });
            }
            else
            {
                Guid orderGuid;
                using (IBatchOperationHandle batchOperation = SessionRepository.StartBatchOperation(session.GameId))
                {
                    orderGuid = await CommandQueue.OrderAttack(batchOperation, session.GameId, session.PhaseId, sourceRegion.RegionId, sourceRegion.CurrentEtag, targetRegion.RegionId, numberOfTroops);
                    await batchOperation.CommitBatch();
                }

                return orderGuid;
            }
        }

        // POST /api/Region/Redeploy
        [Route("Redeploy")]
        public async Task<Guid> PostRedeployTroops(Guid sessionId, Guid regionId, uint numberOfTroops, Guid targetRegionId)
        {
            IRegionData sourceRegion = await RegionRepository.GetRegionOrThrow(sessionId, regionId)
                                                       .IsRegionOwnerOrThrow(User.Identity.GetUserId());
            ISession session = await SessionRepository.GetSessionOrThrow(sourceRegion)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Redeployment);
            IRegionData targetRegion = await RegionRepository.GetRegionOrThrow(session.GameId, targetRegionId)
                                                             .IsRegionOwnerOrThrow(User.Identity.GetUserId())
                                                             .IsRegionConnectedOrThrow(sourceRegion.RegionId);
            INationData nation = await NationRepository.GetNation(sessionId, User.Identity.GetUserId());

            if (sourceRegion.TroopCount <= numberOfTroops)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "You do not have that many troops available to redeploy" });
            }
            else
            {
                return await CommandQueue.Redeploy(session.GameId, session.PhaseId, nation.CurrentEtag, sourceRegion.RegionId, targetRegion.RegionId, numberOfTroops);
            }
        }

        private ICommandQueue CommandQueue { get; set; }
        private INationRepository NationRepository { get; set; }
        private IRegionRepository RegionRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
    }
}
