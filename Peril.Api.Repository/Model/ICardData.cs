using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface ICardData
    {
        Guid RegionId { get; }

        String OwnerId { get; }

        UInt32 Value { get; }

        String CurrentEtag { get; }
    }
}
