using System;

namespace Peril.Core
{
    public enum CombatArmyMode
    {
        Attacking,
        Defending
    }

    public interface ICombatArmy
    {
        Guid OriginRegionId { get; }

        Guid OwnerUserId { get; }

        CombatArmyMode ArmyMode { get; }

        UInt32 NumberOfTroops { get; }
    }
}
