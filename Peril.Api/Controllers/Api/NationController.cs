using Microsoft.AspNet.Identity;
using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            IEnumerable<ICardData> playerCards = await NationRepository.GetCards(sessionId, User.Identity.GetUserId());
            return playerCards.Select(card => new Card(card));
        }

        // POST /api/Nation/Cards
        [Route("Cards")]
        public async Task PostCards(Guid sessionId, IEnumerable<Guid> cards)
        {
            ISession session = await SessionRepository.GetSessionOrThrow(sessionId)
                                                      .IsUserIdJoinedOrThrow(NationRepository, User.Identity.GetUserId())
                                                      .IsPhaseTypeOrThrow(SessionPhase.Reinforcements);

            Task<INationData> nationDataTask = NationRepository.GetNationOrThrow(sessionId, User.Identity.GetUserId());
            Task<IEnumerable<ICardData>> playerCardsTask = NationRepository.GetCards(sessionId, User.Identity.GetUserId());
            if (cards == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "Invalid card(s) specified" });
            }

            HashSet<Guid> cardRegionIds = new HashSet<Guid>();
            foreach(Guid cardRegionId in cards)
            {
                if(cardRegionIds.Contains(cardRegionId))
                {
                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "Duplicate card(s) specified" });
                }
                else if(cardRegionIds.Count >= 3)
                {
                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "Too many card(s) specified" });
                }
                else
                {
                    cardRegionIds.Add(cardRegionId);
                }
            }

            if (cardRegionIds.Count != 3)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "Too few card(s) specified" });
            }

            using (IBatchOperationHandle batchOperation = SessionRepository.StartBatchOperation(sessionId))
            {
                IEnumerable<ICardData> playerCards = await playerCardsTask;
                HashSet<UInt32> seenValues = new HashSet<UInt32>();
                foreach(ICardData card in playerCards)
                {
                    if(cardRegionIds.Contains(card.RegionId))
                    {
                        NationRepository.SetCardDiscarded(batchOperation, sessionId, card.RegionId, card.CurrentEtag);
                        seenValues.Add(card.Value);
                    }
                }

                UInt32 additionalReinforcements = 0;
                if(seenValues.Count == 1)
                {
                    additionalReinforcements = seenValues.First();
                }
                else if (seenValues.Count == 3)
                {
                    additionalReinforcements = 9;
                }
                else
                {
                    await batchOperation.Abort();
                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "Invalid combination of card(s) specified" });
                }

                INationData nation = await nationDataTask;
                NationRepository.SetAvailableReinforcements(batchOperation, sessionId, nation.UserId, nation.CurrentEtag, nation.AvailableReinforcements + additionalReinforcements);
            }
        }

        private INationRepository NationRepository { get; set; }
        private ISessionRepository SessionRepository { get; set; }
    }
}
