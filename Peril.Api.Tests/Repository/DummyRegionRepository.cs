using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Peril.Api.Tests.Repository
{
    class DummyRegionRepository : IRegionRepository
    {
        public DummyRegionRepository()
        {
            RegionData = new Dictionary<Guid, DummyRegionData>();
        }

        public String WorldDefinitionPath { get; set; }

        public Task CreateRegion(Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions)
        {
            RegionData[regionId] = new DummyRegionData(sessionId, regionId, continentId, String.Empty);
            foreach(Guid connectedRegion in connectedRegions)
            {
                RegionData[regionId].ConnectedRegionIds.Add(connectedRegion);
            }

            return Task.FromResult(false);
        }

        public Task<IRegionData> GetRegion(Guid sessionId, Guid regionId)
        {
            if(RegionData.ContainsKey(regionId))
            {
                return Task.FromResult<IRegionData>(RegionData[regionId]);
            }
            else
            {
                return Task.FromResult<IRegionData>(null);
            }
        }

        public Task<IEnumerable<IRegionData>> GetRegions(Guid sessionId)
        {
            IEnumerable<IRegionData> query = from region in RegionData
                                               where region.Value.SessionId == sessionId
                                               select region.Value;
            return Task.FromResult(query);
        }

        public Task AssignRegionOwnership(Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges)
        {
            foreach(var change in ownershipChanges)
            {
                if (RegionData.ContainsKey(change.Key))
                {
                    DummyRegionData regionData = RegionData[change.Key];
                    if(regionData.SessionId == sessionId)
                    {
                        regionData.OwnerId = change.Value.UserId;
                        regionData.TroopCount = change.Value.TroopCount;
                        regionData.GenerateNewEtag();
                    }
                    else
                    {
                        throw new InvalidOperationException("Region id does not exist in the specified session");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid region id specified");
                }
            }

            return Task.FromResult(false);
        }

        #region - Test Setup Helpers -
        public DummyRegionData SetupRegion(Guid sessionId, Guid regionId, Guid continentId, String initialOwner)
        {
            RegionData[regionId] = new DummyRegionData(sessionId, regionId, continentId, initialOwner);
            return RegionData[regionId];
        }
        #endregion

        public Dictionary<Guid, DummyRegionData> RegionData { get; private set; }
    }

    static class ControllerMockRegionRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupDummyWorldAsTree(this ControllerMockSetupContext setupContext)
        {
            return SetupDummyWorldAsTree(setupContext, setupContext.ControllerMock.OwnerId);
        }

        static public ControllerMockSetupContext SetupDummyWorldAsTree(this ControllerMockSetupContext setupContext, String initialOwnerId)
        {
            Guid continentId = new Guid("8BFE434B-4241-415A-A52A-D7EFB715FF7E");
            setupContext.SetupRegion(DummyWorldRegionA, continentId, initialOwnerId);
            setupContext.SetupRegion(DummyWorldRegionB, continentId, initialOwnerId)
                        .SetupRegionConnection(DummyWorldRegionA);
            setupContext.SetupRegion(DummyWorldRegionC, continentId, initialOwnerId)
                        .SetupRegionConnection(DummyWorldRegionB);
            setupContext.SetupRegion(DummyWorldRegionD, continentId, initialOwnerId)
                        .SetupRegionConnection(DummyWorldRegionA);
            setupContext.SetupRegion(DummyWorldRegionE, continentId, initialOwnerId)
                        .SetupRegionConnection(DummyWorldRegionD);

            return setupContext;
        }

        static public ControllerMockSetupContext SetupDummyWorldFromFile(this ControllerMockSetupContext setupContext, String path)
        {
            XDocument worldDefinition = XDocument.Load(path);
            List<Region> regions = worldDefinition.LoadRegions();
            worldDefinition.LoadRegionConnections(regions);
            foreach (IRegion region in regions)
            {
                setupContext.SetupRegion(Guid.NewGuid(), region.ContinentId, setupContext.ControllerMock.OwnerId);
                setupContext.DummyRegion.ConnectedRegionIds = region.ConnectedRegions.ToList();
            }
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegion(this ControllerMockSetupContext setupContext, Guid regionId, Guid continentId)
        {
            return SetupRegion(setupContext, regionId, continentId, setupContext.ControllerMock.OwnerId);
        }

        static public ControllerMockSetupContext SetupRegion(this ControllerMockSetupContext setupContext, Guid regionId, Guid continentId, String initialOwnerId)
        {
            setupContext.DummyRegion = setupContext.ControllerMock.RegionRepository.SetupRegion(setupContext.DummySession.GameId, regionId, continentId, initialOwnerId);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegionConnection(this ControllerMockSetupContext setupContext, Guid otherRegion)
        {
            setupContext.DummyRegion.SetupRegionConnection(setupContext.ControllerMock.RegionRepository.RegionData[otherRegion]);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegionOwnership(this ControllerMockSetupContext setupContext, String ownerId)
        {
            setupContext.DummyRegion.OwnerId = ownerId;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegionOwnership(this ControllerMockSetupContext setupContext, Guid regionId, String ownerId)
        {
            setupContext.DummyRegion = setupContext.ControllerMock.RegionRepository.RegionData[regionId];
            setupContext.SetupRegionOwnership(ownerId);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegionTroops(this ControllerMockSetupContext setupContext, UInt32 troopCount)
        {
            setupContext.DummyRegion.TroopCount = troopCount;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegionTroops(this ControllerMockSetupContext setupContext, Guid regionId, UInt32 troopCount)
        {
            setupContext.DummyRegion = setupContext.ControllerMock.RegionRepository.RegionData[regionId];
            setupContext.SetupRegionTroops(troopCount);
            return setupContext;
        }

        static public Guid DummyWorldRegionA { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        static public Guid DummyWorldRegionB { get { return new Guid("711E2869-22C6-4994-BCC1-26B490A56CC2"); } }
        static public Guid DummyWorldRegionC { get { return new Guid("24BF6F0E-3395-49FC-B055-FA1F91594F35"); } }
        static public Guid DummyWorldRegionD { get { return new Guid("AA064349-D1D6-4CF1-9B26-F81F2C450B8C"); } }
        static public Guid DummyWorldRegionE { get { return new Guid("C667E285-52B0-4B63-867E-E3D390BBCCEA"); } }
    }
}
