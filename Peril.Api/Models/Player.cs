using Peril.Api.Repository;
using Peril.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Models
{
    public class Player : IPlayer
    {
        public String UserId { get; set; }

        public String Name { get; set; }

        public PlayerColour Colour { get; set; }
    }

    static public class NationRepositoryExtensionMethods
    {
        static public async Task<INationData> GetNationOrThrow(this INationRepository repository, Guid sessionId, String userId)
        {
            INationData playerInSession = await repository.GetNation(sessionId, userId);
            if (playerInSession == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "You have not joined the specified session" });
            }
            return playerInSession;
        }
    }
}
