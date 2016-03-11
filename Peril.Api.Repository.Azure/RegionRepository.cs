using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class RegionRepository : IRegionRepository
    {
        public RegionRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            RegionTable = TableClient.GetTableReference("Regions");
            RegionTable.CreateIfNotExists();
        }

        public String WorldDefinitionPath
        {
            get { return System.Web.Hosting.HostingEnvironment.MapPath("~/Content/WorldDefinition.xml"); }
    }

        public async Task CreateRegion(Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions)
        {
            // Create a new table entry
            RegionTableEntry newRegion = new RegionTableEntry(sessionId, regionId, continentId, name);
            newRegion.SetConnectedRegions(connectedRegions);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newRegion);
            await RegionTable.ExecuteAsync(insertOperation);
        }

        public async Task<IRegionData> GetRegion(Guid regionId)
        {
            TableQuery<RegionTableEntry> query = new TableQuery<RegionTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, regionId.ToString()));

            var results = await RegionTable.ExecuteQuerySegmentedAsync(query, null);
            return results.FirstOrDefault();
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable RegionTable { get; set; }
    }
}
