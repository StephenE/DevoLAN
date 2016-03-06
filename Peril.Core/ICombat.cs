using System;
using System.Collections.Generic;

namespace Peril.Core
{
    public enum CombatType
    {
        BorderClash,
        MassInvasion,
        Invasion,
        SpoilsOfWar
    }

    public interface ICombat
    {
        Guid CombatId { get; }

        CombatType ResolutionType { get; }

        IEnumerable<ICombatArmy> InvolvedArmies { get; }
    }
}
