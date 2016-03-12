using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;

namespace Peril.Api.Repository.Azure.Model
{
    public class NationTableEntry : TableEntity, IPlayer
    {
        public NationTableEntry(Guid sessionId, String userId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = userId.ToString();
            CompletedPhase = Guid.Empty;
            ColourId = 0;
        }

        public NationTableEntry()
        {

        }

        public Guid SessionId
        {
            get { return Guid.Parse(PartitionKey); }
        }

        public String UserId
        {
            get { return RowKey; }
        }

        public Guid CompletedPhase { get; set; }

        public Int32 ColourId { get; set; }

        public String Name
        {
            get { throw new NotImplementedException("Name property is stored in the accounts database, not the Azure table"); }
        }

        public PlayerColour Colour
        {
            get { return (PlayerColour)ColourId; }
        }
    }
}
