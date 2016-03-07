using Peril.Core;
using System;

namespace Peril.Api.Repository
{
    public interface IRegionData : IRegion
    {
        Guid SessionId { get; }

        UInt32 TroopsCommittedToPhase { get; }

        String CurrentEtag { get; }
    }
}
