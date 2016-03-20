﻿using Peril.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface IWorldRepository
    {
        Task<IEnumerable<ICombat>> GetCombat(Guid sessionId);

        Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType type);

        Task AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies);

        Task AddArmyToCombat(Guid sessionId, IDictionary<Guid, IEnumerable<ICombatArmy>> armies);

        Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results);

        IEnumerable<Int32> GetRandomNumberGenerator(Guid targetRegion, int minimum, int maximum);
    }
}
