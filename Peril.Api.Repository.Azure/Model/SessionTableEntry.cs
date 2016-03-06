using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;

namespace Peril.Api.Repository.Azure.Model
{
    public class SessionTableEntry : TableEntity, ISession
    {
        public SessionTableEntry(Guid sessionId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = sessionId.ToString();
            PhaseId = Guid.Empty;
            PhaseType = SessionPhase.NotStarted;
        }

        public SessionTableEntry()
        {

        }

        public Guid GameId { get { return Guid.Parse(PartitionKey); } }
        public String OwnerUserId { get; set; }
        public Guid PhaseId { get; set; }
        public SessionPhase PhaseType { get; set; }
    }
}
