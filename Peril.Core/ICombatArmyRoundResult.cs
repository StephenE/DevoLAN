using System;
using System.Collections.Generic;

namespace Peril.Core
{
    public interface ICombatArmyRoundResult
    {
        Guid OriginRegionId { get; }

        String OwnerUserId { get; }

        IEnumerable<UInt32> RolledResults { get; }

        UInt32 TroopsLost { get; }
    }
}
