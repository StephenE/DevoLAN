using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Repository
{
    public interface IWorldRepository
    {
        IEnumerable<ICombat> GetCombat(Guid sessionId);
    }
}
