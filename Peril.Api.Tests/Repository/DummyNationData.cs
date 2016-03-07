using Peril.Api.Repository;
using System;

namespace Peril.Api.Tests.Repository
{
    class DummyNationData : INationData
    {
        public DummyNationData(String userId)
        {
            UserId = userId;
            AvailableReinforcements = 0;
            CurrentEtag = "Initial-Etag";
        }

        public String UserId { get; private set; }

        public UInt32 AvailableReinforcements { get; set; }

        public String CurrentEtag { get; set; }
    }
}
