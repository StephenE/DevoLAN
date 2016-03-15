using Peril.Api.Repository;
using Peril.Core;
using System;

namespace Peril.Api.Tests.Repository
{
    class DummyNationData : INationData
    {
        public DummyNationData(String userId)
        {
            UserId = userId;
            AvailableReinforcements = 0;
            CompletedPhase = Guid.Empty;
            CurrentEtag = "Initial-Etag";
        }

        public String UserId { get; private set; }

        public PlayerColour Colour { get; set; }

        public UInt32 AvailableReinforcements { get; set; }

        public Guid CompletedPhase { get; set; }

        public String CurrentEtag { get; set; }
    }
}
