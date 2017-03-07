using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface IWorldRepository
    {
        Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, UInt32 round);

        Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, UInt32 round, CombatType type);

        void AddCombat(IBatchOperationHandle batchOperationHandle, Guid sessionId, UInt32 round, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies);

        void AddArmyToCombat(IBatchOperationHandle batchOperationHandle, ICombat combat, IEnumerable<ICombatArmy> armies);

        Task AddCombatResults(Guid sessionId, UInt32 round, IEnumerable<ICombatResult> results);

        IEnumerable<Int32> GetRandomNumberGenerator(Guid targetRegion, int minimum, int maximum);
    }
}
