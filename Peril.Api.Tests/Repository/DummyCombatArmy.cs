using Peril.Core;
using System;

namespace Peril.Api.Tests.Repository
{
    public class DummyCombatArmy : ICombatArmy
    {
        public DummyCombatArmy(Guid originRegion, String ownerId, CombatArmyMode mode, UInt32 numberOfTroops)
        {
            OriginRegionId = originRegion;
            OwnerUserId = ownerId;
            ArmyMode = mode;
            NumberOfTroops = numberOfTroops;
        }

        public Guid OriginRegionId { get; set; }

        public String OwnerUserId { get; set; }

        public CombatArmyMode ArmyMode { get; set; }

        public UInt32 NumberOfTroops { get; set; }
    }
}
