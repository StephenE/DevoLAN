using System;

namespace Peril.Api.Repository
{
    public interface INationData
    {
        String UserId { get; }

        UInt32 AvailableReinforcements { get; }

        String CurrentEtag { get; }
    }
}
