using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Model
{
    public class OwnershipChange
    {
        public OwnershipChange(String userId, UInt32 troopCount)
        {
            UserId = userId;
            TroopCount = troopCount;
        }

        public String UserId { get; set; }
        public UInt32 TroopCount { get; set; }
    }
}
