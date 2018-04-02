using Peril.Api.Repository;
using System;

namespace Peril.Api.Tests.Repository
{
    public class DummyCardData : ICardData
    {
        static public String UnownedCard { get { return "Unowned"; } }
        static public String UsedCard { get { return "Used"; } }

        public Guid RegionId { get; internal set; }
        public String OwnerId { get; internal set; }
        public UInt32 Value { get; internal set; }
        public String CurrentEtag { get; internal set; }

        public DummyCardData(Guid regionId, UInt32 value)
        {
            RegionId = regionId;
            OwnerId = UnownedCard;
            Value = value;
            CurrentEtag = "Initial-Etag";
        }
    }
}
