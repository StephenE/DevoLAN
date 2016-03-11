using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Model
{
    class RegionTableEntry : TableEntity
    {
        public RegionTableEntry(Guid sessionId, Guid regionId, Guid continentId, String name)
        {
            PartitionKey = sessionId.ToString();
            RowKey = regionId.ToString();
            ContinentId = continentId;
            Name = name;
            OwnerId = String.Empty;
            TroopCount = 0;
            ConnectedRegionList = new List<Guid>();
        }

        public RegionTableEntry()
        {

        }

        public Guid SessionId
        {
            get { return Guid.Parse(PartitionKey); }
        }

        public Guid RegionId
        {
            get { return Guid.Parse(RowKey); }
        }

        public Guid ContinentId { get; set; }

        public String Name { get; set; }

        public IEnumerable<Guid> ConnectedRegions
        {
            get { return ConnectedRegionList; }
        }

        public String OwnerId { get; set; }

        public UInt32 TroopCount { get; set; }

        public List<Guid> ConnectedRegionList { get; set; }
    }
}
