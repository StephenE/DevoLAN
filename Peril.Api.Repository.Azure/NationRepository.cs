using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class NationRepository : INationRepository
    {
        public NationRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
        }

        public async Task<INationData> GetNation(Guid sessionId, string userId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            var operation = TableOperation.Retrieve<NationTableEntry>(sessionId.ToString(), "Nation_" + userId);
            var result = await sessionDataTable.ExecuteAsync(operation);
            NationTableEntry typedResult = result.Result as NationTableEntry;
            if (typedResult != null)
            {
                typedResult.IsValid();
            }
            return typedResult;
        }

        public async Task<IEnumerable<INationData>> GetNations(Guid sessionId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            var rowKeyCondition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "Nation_"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "Nation`")
            );

            List<INationData> results = new List<INationData>();
            TableQuery<NationTableEntry> query = new TableQuery<NationTableEntry>()
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

            foreach(NationTableEntry entry in results)
            {
                entry.IsValid();
            }

            return results;
        }

        public async Task MarkPlayerCompletedPhase(Guid sessionId, string userId, Guid phaseId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Fetch existing entry
            var operation = TableOperation.Retrieve<NationTableEntry>(sessionId.ToString(), "Nation_" + userId);
            var result = await sessionDataTable.ExecuteAsync(operation);

            // Modify entry
            NationTableEntry playerEntry = result.Result as NationTableEntry;
            playerEntry.IsValid();
            playerEntry.CompletedPhase = phaseId;

            // Write entry back (fails on write conflict)
            try
            {
                TableOperation insertOperation = TableOperation.Replace(playerEntry);
                await sessionDataTable.ExecuteAsync(insertOperation);
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    throw new ConcurrencyException();
                }
                else
                {
                    throw exception;
                }
            }
        }

        public async Task SetAvailableReinforcements(Guid sessionId, Dictionary<String, UInt32> reinforcements)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Fetch all nations (quicker than fetching only what we need, one by one)
            var nations = await GetNations(sessionId);

            // Get NationTableEntry for every player that needs updating
            var updateOperations = from playerEntry in reinforcements
                                   join nation in nations.Cast<NationTableEntry>() on playerEntry.Key equals nation.UserId
                                   select new { Nation = nation, Reinforcements = (Int32)playerEntry.Value };

            // Modify entries as required
            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var operation in updateOperations)
            {
                NationTableEntry nationEntry = operation.Nation;
                nationEntry.IsValid();
                nationEntry.AvailableReinforcementsRaw = operation.Reinforcements;
                batchOperation.Replace(nationEntry);
            }

            // Write entry back (fails on write conflict)
            try
            {
                await sessionDataTable.ExecuteBatchAsync(batchOperation);
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    throw new ConcurrencyException();
                }
                else
                {
                    throw exception;
                }
            }
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
    }
}
