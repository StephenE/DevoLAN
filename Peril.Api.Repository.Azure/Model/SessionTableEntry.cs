using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;

namespace Peril.Api.Repository.Azure.Model
{
    public class SessionTableEntry : TableEntity, ISession
    {
        public SessionTableEntry(String ownerId, Guid sessionId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = ownerId;
            PhaseId = Guid.Empty;
            PhaseType = SessionPhase.NotStarted;
        }

        public SessionTableEntry()
        {

        }

        public Guid GameId { get { return Guid.Parse(PartitionKey); } }
        public String OwnerUserId { get { return RowKey; } }
        public Guid PhaseId { get; set; }
        public SessionPhase PhaseType { get; set; }
    }
}
