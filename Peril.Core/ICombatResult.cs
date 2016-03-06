using System;
using System.Collections.Generic;

namespace Peril.Core
{
    public interface ICombatResult
    {
        Guid CombatId { get; }

        IEnumerable<ICombatRoundResult> Rounds { get; }
    }
}
