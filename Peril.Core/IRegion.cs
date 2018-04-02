using System;
using System.Collections.Generic;

namespace Peril.Core
{
    public interface IRegion
    {
        Guid RegionId { get; }

        Guid ContinentId { get; }

        String Name { get; }

        IEnumerable<Guid> ConnectedRegions { get; }

        String OwnerId { get; }

        UInt32 TroopCount { get; }

        UInt32 CardValue { get; }
    }
}
