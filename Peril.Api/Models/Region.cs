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

namespace Peril.Api.Models
{
    public class Region : IRegion
    {
        public Region()
        {

        }

        public Region(IRegionData data)
        {
            RegionId = data.RegionId;
            ContinentId = data.ContinentId;
            Name = data.Name;
            ConnectedRegions = data.ConnectedRegions;
            OwnerId = data.OwnerId;
            TroopCount = data.TroopCount;
            CardValue = data.CardValue;
        }

        public Guid RegionId { get; set; }

        public Guid ContinentId { get; set; }

        public String Name { get; set; }

        public IEnumerable<Guid> ConnectedRegions { get; set; }

        public String OwnerId { get; set; }

        public UInt32 TroopCount { get; set; }

        public UInt32 CardValue { get; set; }
    }

    static public class RegionRepositoryExtensionMethods
    {
        static public async Task<IRegionData> GetRegionOrThrow(this IRegionRepository repository, Guid sessionId, Guid regionId)
        {
            IRegionData region = await repository.GetRegion(sessionId, regionId);
            if (region == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No region found with the provided Guid" });
            }
            return region;
        }

        static public async Task<IRegionData> IsRegionOwnerOrThrow(this Task<IRegionData> regionTask, String userId)
        {
            IRegionData region = await regionTask;
            if (region.OwnerId != userId)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotAcceptable, ReasonPhrase = "You are not the owner of the region" });
            }
            return region;
        }

        static public async Task<IRegionData> IsNotRegionOwnerOrThrow(this Task<IRegionData> regionTask, String userId)
        {
            IRegionData region = await regionTask;
            if (region.OwnerId == userId)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotAcceptable, ReasonPhrase = "You are the owner of the region" });
            }
            return region;
        }

        static public async Task<IRegionData> IsRegionConnectedOrThrow(this Task<IRegionData> regionTask, Guid connectedRegionId)
        {
            IRegionData region = await regionTask;
            if (!region.ConnectedRegions.Contains(connectedRegionId))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.PaymentRequired, ReasonPhrase = "The two regions are not connected" });
            }
            return region;
        }

        static public async Task<ISessionData> GetSessionOrThrow(this ISessionRepository repository, IRegionData region)
        {
            ISessionData session = await repository.GetSession(region.SessionId);
            if (session == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found for the provided region" });
            }
            return session;
        }
    }
}