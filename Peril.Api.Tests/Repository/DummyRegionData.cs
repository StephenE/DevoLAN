using Peril.Api.Repository;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    class DummyRegionData : IRegionData
    {
        public DummyRegionData(Guid sessionId, Guid regionId, Guid continentId, String initialOwner, UInt32 cardValue)
        {
            SessionId = sessionId;
            RegionId = regionId;
            ContinentId = continentId;
            OwnerId = initialOwner;
            TroopCount = 0;
            TroopsCommittedToPhase = 0;
            CardValue = cardValue;
            GenerateNewEtag();
        }

        public Guid RegionId { get; private set; }

        public Guid ContinentId { get; private set; }

        public String Name { get { return RegionId.ToString(); } }

        public IEnumerable<Guid> ConnectedRegions { get { return ConnectedRegionIds; } }

        public String OwnerId { get; set; }

        public UInt32 TroopCount { get; set; }

        public UInt32 CardValue { get; set; }

        public Guid SessionId { get; private set; }

        public UInt32 TroopsCommittedToPhase { get; set; }

        public String CurrentEtag { get; set; }

        public List<Guid> ConnectedRegionIds = new List<Guid>();

        #region - Test Setup Helpers -
        public DummyRegionData SetupRegionConnection(DummyRegionData otherRegion)
        {
            ConnectedRegionIds.Add(otherRegion.RegionId);
            otherRegion.ConnectedRegionIds.Add(RegionId);
            return this;
        }

        internal DummyRegionData GenerateNewEtag()
        {
            CurrentEtag = Guid.NewGuid().ToString();
            return this;
        }
        #endregion
    }
}
