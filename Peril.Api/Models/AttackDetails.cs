using System;

namespace Peril.Api.Models
{
    public class AttackDetails
    {
        public Guid SourceRegion { get; set; }

        public Guid TargetRegion { get; set; }

        public UInt32 NumberOfTroops { get; set; }
    }
}