using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
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

        public async Task<IRegionData> GetRegion(Guid sessionId, Guid regionId)
        {
            TableOperation operation = TableOperation.Retrieve<RegionTableEntry>(sessionId.ToString(), regionId.ToString());
            TableResult result = await RegionTable.ExecuteAsync(operation);
            return result.Result as RegionTableEntry;
        }

        public async Task<IEnumerable<IRegionData>> GetRegions(Guid sessionId)
        {
            List<RegionTableEntry> results = new List<RegionTableEntry>();
            TableQuery<RegionTableEntry> query = new TableQuery<RegionTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            do
            {
                var queryResults = await RegionTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task AssignRegionOwnership(Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges)
        {
            throw new NotImplementedException("Not implemented");
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable RegionTable { get; set; }
    }
}
