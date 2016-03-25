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
using System.Xml.Linq;

namespace Peril.Api.Models
{
    public class Session : ISession
    {
        public Session()
        {

        }

        public Session(ISession repositorySession)
        {
            GameId = repositorySession.GameId;
            PhaseId = repositorySession.PhaseId;
            PhaseType = repositorySession.PhaseType;
            OwnerId = repositorySession.OwnerId;
            Round = repositorySession.Round;
        }

        public Guid GameId { get; set; }
        public Guid PhaseId { get; set; }
        public String OwnerId { get; set; }
        public UInt32 Round { get; set; }
        public SessionPhase PhaseType { get; set; }
    }

    static public class SessionRepositoryExtensionMethods
    {
        static public async Task<ISessionData> GetSessionOrThrow(this ISessionRepository repository, Guid sessionId)
        {
            ISessionData session = await repository.GetSession(sessionId);
            if (session == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, ReasonPhrase = "No session found with the provided Guid" });
            }
            return session;
        }

        static public async Task<ISessionData> IsPhaseIdOrThrow(this Task<ISessionData> sessionTask, Guid phaseId)
        {
            ISessionData session = await sessionTask;
            if (session.PhaseId != phaseId)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = "The session is no longer in the specified phase" });
            }
            return session;
        }

        static public async Task<ISessionData> IsPhaseTypeOrThrow(this Task<ISessionData> sessionTask, SessionPhase phaseType)
        {
            ISessionData session = await sessionTask;
            if (session.PhaseType != phaseType)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.ExpectationFailed, ReasonPhrase = String.Format("The session is no longer in the {0} phase", phaseType) });
            }
            return session;
        }

        static public async Task<ISessionData> IsUserIdJoinedOrThrow(this Task<ISessionData> sessionTask, INationRepository repository, String userId)
        {
            ISessionData session = await sessionTask;
            INationData playerInSession = await repository.GetNation(session.GameId, userId);
            if (playerInSession == null)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "You have not joined the specified session" });
            }
            return session;
        }

        static public async Task<ISessionData> IsSessionOwnerOrThrow(this Task<ISessionData> sessionTask, String userId)
        {
            ISessionData session = await sessionTask;
            if (session.OwnerId != userId)
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "Only the session owner is allowed to take this action" });
            }
            return session;
        }

        static public List<Region> LoadRegions(this XDocument worldDefinition)
        {
            var regionsList = from continentXml in worldDefinition.Root.Elements("Continent")
                              let continentId = Guid.NewGuid()
                              from regionXml in continentXml.Elements("Region")
                              select new Region
                              {
                                  RegionId = Guid.NewGuid(),
                                  ContinentId = continentId,
                                  Name = regionXml.Attribute("Name").Value
                              };
            return regionsList.ToList();
        }

        static public void LoadRegionConnections(this XDocument worldDefinition, List<Region> regions)
        {
            Dictionary<String, Region> regionLookup = new Dictionary<string, Region>();
            Dictionary<String, List<Guid>> regionConnectionsLookup = new Dictionary<string, List<Guid>>();
            foreach (Region region in regions)
            {
                regionLookup[region.Name] = region;
                regionConnectionsLookup[region.Name] = new List<Guid>();
            }

            var connections = from connectionXml in worldDefinition.Root.Elements("Connections")
                              from connectedXml in connectionXml.Elements("Connected")
                              let regionId = connectedXml.Attribute("Name").Value
                              let otherRegionId = connectedXml.Attribute("Other").Value
                              join regionData in regions on regionId equals regionData.Name
                              join otherRegionData in regions on otherRegionId equals otherRegionData.Name
                              select new
                              {
                                  Region = regionId,
                                  RegionId = regionData.RegionId,
                                  OtherRegion = otherRegionId,
                                  OtherRegionId = otherRegionData.RegionId
                              };


            foreach (var connection in connections)
            {
                regionConnectionsLookup[connection.Region].Add(connection.OtherRegionId);
                regionConnectionsLookup[connection.OtherRegion].Add(connection.RegionId);
            }

            foreach(var connectionEntry in regionConnectionsLookup)
            {
                regionLookup[connectionEntry.Key].ConnectedRegions = connectionEntry.Value;
            }
        }
    }
}