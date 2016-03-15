using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;

namespace Peril.Api.Repository.Azure.Model
{
    public class NationTableEntry : TableEntity, INationData
    {
        public NationTableEntry(Guid sessionId, String userId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = userId.ToString();
            CompletedPhase = Guid.Empty;
            ColourId = 0;
            AvailableReinforcementsRaw = 0;
        }

        public NationTableEntry()
        {

        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public String UserId { get { return RowKey; } }
        public String CurrentEtag { get { return ETag; } }
        public uint AvailableReinforcements { get { return (UInt32)AvailableReinforcementsRaw; } }

        public Guid CompletedPhase { get; set; }

        public Int32 ColourId { get; set; }

        public Int32 AvailableReinforcementsRaw { get; set; }

        public PlayerColour Colour
        {
            get { return (PlayerColour)ColourId; }
        }
    }
}
