using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class RegionRepository : IRegionRepository
    {
        public RegionRepository(String storageConnectionString, String worldDefinitionPath)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            WorldDefinitionPath = worldDefinitionPath;
        }

        public String WorldDefinitionPath { get; private set; }

        public async Task CreateRegion(Guid sessionId, Guid regionId, Guid continentId, String name, IEnumerable<Guid> connectedRegions)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Create a new table entry
            RegionTableEntry newRegion = new RegionTableEntry(sessionId, regionId, continentId, name);
            newRegion.IsValid();
            newRegion.SetConnectedRegions(connectedRegions);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newRegion);
            await sessionDataTable.ExecuteAsync(insertOperation);
        }

        public async Task<IRegionData> GetRegion(Guid sessionId, Guid regionId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            TableOperation operation = TableOperation.Retrieve<RegionTableEntry>(sessionId.ToString(), "Region_" + regionId.ToString());
            TableResult result = await sessionDataTable.ExecuteAsync(operation);
            var typedResult = result.Result as RegionTableEntry;
            if (typedResult != null)
            {
                typedResult.IsValid();
            }
            return typedResult;
        }

        public async Task<IEnumerable<IRegionData>> GetRegions(Guid sessionId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            var rowKeyCondition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "Region_"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "Region`")
            );

            List<RegionTableEntry> results = new List<RegionTableEntry>();
            TableQuery<RegionTableEntry> query = new TableQuery<RegionTableEntry>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()),
                    TableOperators.And,
                    rowKeyCondition
                ));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            do
            {
                var queryResults = await sessionDataTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            foreach(RegionTableEntry region in results)
            {
                region.IsValid();
            }

            return results;
        }

        public void AssignRegionOwnership(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Dictionary<Guid, OwnershipChange> ownershipChanges)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;

            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Fetch all regions (quicker than fetching only what we need, one by one)
            var updateRegionOwnershipTask = GetRegions(sessionId)
                .ContinueWith(regionTask =>
                {
                    // Get NationTableEntry for every player that needs updating
                    var regions = regionTask.Result;
                    var updateOperations = from ownershipChange in ownershipChanges
                                           join region in regions.Cast<RegionTableEntry>() on ownershipChange.Key equals region.RegionId
                                           select new { Region = region, NewOwner = ownershipChange.Value.UserId, NewTroopCount = (Int32)ownershipChange.Value.TroopCount };

                    // Modify entries as required
                    foreach (var operation in updateOperations)
                    {
                        RegionTableEntry regionEntry = operation.Region;
                        regionEntry.IsValid();
                        regionEntry.OwnerId = operation.NewOwner;
                        regionEntry.StoredTroopCount = operation.NewTroopCount;
                        regionEntry.StoredTroopsCommittedToPhase = 0;
                        batchOperationHandle.BatchOperation.Replace(regionEntry);
                    }
                });

            batchOperationHandle.AddPrerequisite(updateRegionOwnershipTask, ownershipChanges.Count);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
    }
}
