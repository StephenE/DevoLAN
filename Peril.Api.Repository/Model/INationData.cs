using Peril.Core;
using System;

namespace Peril.Api.Repository
{
    public interface INationData
    {
        String UserId { get; }

        PlayerColour Colour { get; }

        UInt32 AvailableReinforcements { get; }

        String CurrentEtag { get; }
    }
}
