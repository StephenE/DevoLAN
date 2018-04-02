using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Model
{
    public class CardTableEntry : TableEntity, ICardData
    {
        public enum State
        {
            Unowned,
            Owned,
            Discarded
        }

        static public DynamicTableEntity CreateDynamicTableEntity(Guid sessionId, Guid regionId, String currentEtag)
        {
            DynamicTableEntity tableEntity = new DynamicTableEntity(sessionId.ToString(), "Card_" + regionId.ToString());
            tableEntity.ETag = currentEtag;
            return tableEntity;
        }

        static public void SetOwner(DynamicTableEntity tableEntry, State ownerState, String ownerId)
        {
            tableEntry.Properties.Add("OwnerStateRaw", new EntityProperty((Int32)ownerState));
            if(ownerState == State.Owned)
            {
                tableEntry.Properties.Add("OwnerId", new EntityProperty(ownerId));
            }
            else if (ownerState == State.Unowned)
            {
                tableEntry.Properties.Add("OwnerId", new EntityProperty(String.Empty));
            }
        }

        public CardTableEntry(Guid sessionId, Guid regionId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = "Card_" + regionId.ToString();
            OwnerStateRaw = (Int32)State.Unowned;
        }

        public CardTableEntry()
        {
        }

        [Conditional("DEBUG")]
        public void IsValid()
        {
            if (!RowKey.StartsWith("Card_"))
            {
                throw new InvalidOperationException(String.Format("RowKey {0} doesn't start with 'Card_'", RowKey));
            }
        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public Guid RegionId { get { return Guid.Parse(RowKey.Substring(5)); } }
        public String CurrentEtag { get { return ETag; } }
        public String OwnerId { get; set; }
        public UInt32 Value { get { return (UInt32)ValueRaw; } }
        public State OwnerState { get { return (State)OwnerStateRaw; } }

        public Int32 ValueRaw { get; set; }
        public Int32 OwnerStateRaw { get; set; }
    }
}
