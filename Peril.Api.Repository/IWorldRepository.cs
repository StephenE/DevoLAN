using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface IWorldRepository
    {
        Task<IEnumerable<ICombat>> GetCombat(Guid sessionId);

        Task AddCombat(IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies);
    }
}
