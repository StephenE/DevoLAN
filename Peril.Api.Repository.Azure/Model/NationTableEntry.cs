﻿using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;
using System.Diagnostics;

namespace Peril.Api.Repository.Azure.Model
{
    public class NationTableEntry : TableEntity, INationData
    {
        public NationTableEntry(Guid sessionId, String userId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = "Nation_" + userId.ToString();
            CompletedPhase = Guid.Empty;
            ColourId = 0;
            AvailableReinforcementsRaw = 0;
        }

        public NationTableEntry()
        {

        }

        [Conditional("DEBUG")]
        public void IsValid()
        {
            if(!RowKey.StartsWith("Nation_"))
            {
                throw new InvalidOperationException(String.Format("RowKey {0} doesn't start with 'Nation_'", RowKey));
            }
        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public String UserId { get { return RowKey.Substring(7); } }
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
