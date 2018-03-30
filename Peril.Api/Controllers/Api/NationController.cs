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
    [RoutePrefix("api/Nation")]
    public class NationController : ApiController
    {
        public NationController(INationRepository nationRepository, ISessionRepository sessionRepository)
        {
            NationRepository = nationRepository;
            SessionRepository = sessionRepository;
        }

        // GET /api/Nation/Reinforcements
        [Route("Reinforcements")]
        public async Task<UInt32> GetReinforcements(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsPhaseTypeOrThrow(SessionPhase.Reinforcements);
            INationData nation = await NationRepository.GetNationOrThrow(sessionId, User.Identity.GetUserId());
            return nation.AvailableReinforcements;
        }

        // GET /api/Nation/Cards
        [Route("Cards")]
        public async Task<IEnumerable<ICard>> GetCards(Guid sessionId)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId());

            throw new NotImplementedException("To do");
        }

        // POST /api/Nation/Cards
        [Route("Cards")]
        public async Task PostCards(Guid sessionId, IEnumerable<Guid> cards)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Reinforcements);

            throw new NotImplementedException("To do");
        }

        private INationRepository NationRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
    }
}
