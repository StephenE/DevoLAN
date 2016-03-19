using System;
using System.Collections.Generic;

namespace Peril.Api.Models
{
    public class CombatOrderRegion
    {
        public CombatOrderRegion()
        {
            OutgoingArmies = new Dictionary<Guid, UInt32>();
        }

        public Guid RegionId { get; set; }
        public UInt32 TroopCount { get; set; }
        public String CurrentEtag { get; set; }
        public String OwnerId { get; set; }
        public Dictionary<Guid, UInt32> OutgoingArmies { get; set; }
}
}
