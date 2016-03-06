﻿using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Peril.Api.Repository.Azure.Model
{
    public class SessionPlayerTableEntry : TableEntity
    {
        public SessionPlayerTableEntry(Guid sessionId, String userId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = userId.ToString();
            CompletedPhase = Guid.Empty;
        }

        public SessionPlayerTableEntry()
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
    }
}
