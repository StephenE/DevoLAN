using System.Collections.Generic;

namespace Peril.Core
{
    public interface ICombatRoundResult
    {
        IEnumerable<ICombatArmyRoundResult> ArmyResults { get; }
    }
}
