using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Core
{
    public interface ICard
    {
        Guid RegionId { get; }

        UInt32 Value { get; }
    }
}
