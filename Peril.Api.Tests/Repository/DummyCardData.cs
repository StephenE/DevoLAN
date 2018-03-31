using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    public class DummyCardData : ICardData
    {
        public String UnownedCard { get { return "Unowned"; } }
        public String UsedCard { get { return "Used"; } }

        public Guid RegionId { get; internal set; }
        public String OwnerId { get; internal set; }
        public UInt32 Value { get; internal set; }

        public DummyCardData(Guid regionId, UInt32 value)
        {
            RegionId = regionId;
            OwnerId = UnownedCard;
            Value = value;
        }
    }
}
