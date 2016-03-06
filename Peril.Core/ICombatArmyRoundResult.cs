using System;
using System.Collections.Generic;

namespace Peril.Core
{
    public interface ICombatArmyRoundResult
    {
        Guid OriginRegionId { get; }

        Guid OwnerUserId { get; }

        IEnumerable<UInt32> RolledResults { get; }
    }
}
