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
            CardData = new Dictionary<Guid, DummyCardData>();
        }

        public String WorldDefinitionPath { get; set; }

        public void CreateRegion(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions, UInt32 cardValue)
        {
            DummyBatchOperationHandle batchOperationHandle = batchOperationHandleInterface as DummyBatchOperationHandle;
            batchOperationHandle.QueuedOperations.Add(() =>
            {
                RegionData[regionId] = new DummyRegionData(sessionId, regionId, continentId, String.Empty);
                foreach (Guid connectedRegion in connectedRegions)
                {
                    RegionData[regionId].ConnectedRegionIds.Add(connectedRegion);
                }
            });
            batchOperationHandle.QueuedOperations.Add(() =>
            {
                CardData[regionId] = new DummyCardData(regionId, cardValue);
            });
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

        public void AssignRegionOwnership(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges)
        {
            IEnumerable<IRegionData> regions = null;
            AssignRegionOwnership(batchOperationHandleInterface, regions, ownershipChanges);
        }

        public void AssignRegionOwnership(IBatchOperationHandle batchOperationHandleInterface, IEnumerable<IRegionData> regions, Dictionary<Guid, OwnershipChange> ownershipChanges)
        {
            DummyBatchOperationHandle batchOperationHandle = batchOperationHandleInterface as DummyBatchOperationHandle;
            foreach (var change in ownershipChanges)
            {
                if (RegionData.ContainsKey(change.Key))
                {
                    DummyRegionData regionData = RegionData[change.Key];
                    batchOperationHandle.QueuedOperations.Add(() =>
                    {
                        regionData.OwnerId = change.Value.UserId;
                        regionData.TroopCount = change.Value.TroopCount;
                        regionData.TroopsCommittedToPhase = 0;
                        regionData.GenerateNewEtag();
                    });
                }
                else
                {
                    throw new InvalidOperationException("Invalid region id specified");
                }
            }
        }

        public void CommitTroopsToPhase(IBatchOperationHandle batchOperationHandleInterface, IRegionData sourceRegion, UInt32 troopsToCommit)
        {
            DummyBatchOperationHandle batchOperationHandle = batchOperationHandleInterface as DummyBatchOperationHandle;
            DummyRegionData region = sourceRegion as DummyRegionData;
            if (region != null && region.TroopCount > region.TroopsCommittedToPhase + troopsToCommit)
            {
                batchOperationHandle.QueuedOperations.Add(() =>
                {
                    region.TroopsCommittedToPhase += troopsToCommit;
                });
            }
            else
            {
                throw new InvalidOperationException("Invalid arguments passed");
            }
        }


        #region - Test Setup Helpers -
        public DummyRegionData SetupRegion(Guid sessionId, Guid regionId, Guid continentId, String initialOwner, UInt32 cardValue)
        {
            RegionData[regionId] = new DummyRegionData(sessionId, regionId, continentId, initialOwner);
            CardData[regionId] = new DummyCardData(regionId, cardValue);
            return RegionData[regionId];
        }
        #endregion

        public Dictionary<Guid, DummyRegionData> RegionData { get; private set; }
        public Dictionary<Guid, DummyCardData> CardData { get; private set; }
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

            // TODO: Setup cards in each region to be unowned

            return setupContext;
        }

        static public ControllerMockSetupContext SetupDummyWorldFromFile(this ControllerMockSetupContext setupContext, String path)
        {
            XDocument worldDefinition = XDocument.Load(path);
            List<Region> regions = worldDefinition.LoadRegions();
            worldDefinition.LoadRegionConnections(regions);
            foreach (IRegion region in regions)
            {
                setupContext.SetupRegion(Guid.NewGuid(), region.ContinentId, setupContext.ControllerMock.OwnerId, 0);
                setupContext.DummyRegion.ConnectedRegionIds = region.ConnectedRegions.ToList();
            }
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRegion(this ControllerMockSetupContext setupContext, Guid regionId, Guid continentId, UInt32 cardValue)
        {
            return SetupRegion(setupContext, regionId, continentId, setupContext.ControllerMock.OwnerId, cardValue);
        }

        static public ControllerMockSetupContext SetupRegion(this ControllerMockSetupContext setupContext, Guid regionId, Guid continentId, String initialOwnerId)
        {
            return SetupRegion(setupContext, regionId, continentId, initialOwnerId, ControllerMockRegionRepositoryExtensions.DummyWorldCardValues[regionId]);
        }

        static public ControllerMockSetupContext SetupRegion(this ControllerMockSetupContext setupContext, Guid regionId, Guid continentId, String initialOwnerId, UInt32 cardValue)
        {
            setupContext.DummyRegion = setupContext.ControllerMock.RegionRepository.SetupRegion(setupContext.DummySession.GameId, regionId, continentId, initialOwnerId, cardValue);
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
        static public Dictionary<Guid, UInt32> DummyWorldCardValues { get { return m_DummyWorldCardValues; } }

        static private Dictionary<Guid, UInt32> m_DummyWorldCardValues = new Dictionary<Guid, UInt32>
        {
            {DummyWorldRegionA, 3},
            {DummyWorldRegionB, 5},
            {DummyWorldRegionC, 7},
            {DummyWorldRegionD, 3},
            {DummyWorldRegionE, 3},
        };
    }
}
