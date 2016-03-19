using Peril.Core;
using System;

namespace Peril.Api.Models
{
    class CombatArmy : ICombatArmy
    {
        public CombatArmy(ICombatArmy army)
        {
            OriginRegionId = army.OriginRegionId;
            OwnerUserId = army.OwnerUserId;
            ArmyMode = army.ArmyMode;
            NumberOfTroops = army.NumberOfTroops;
        }

        public CombatArmy(Guid originRegionId, String ownerId, CombatArmyMode mode, UInt32 numberOfTroops)
        {
            OriginRegionId = originRegionId;
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
