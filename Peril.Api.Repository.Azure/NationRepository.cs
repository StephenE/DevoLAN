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
        static public String NationTableName { get { return "SessionPlayers"; } }

        public NationRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            SessionPlayersTable = TableClient.GetTableReference(NationTableName);
            SessionPlayersTable.CreateIfNotExists();
        }

        public async Task<INationData> GetNation(Guid sessionId, string userId)
        {
            var operation = TableOperation.Retrieve<NationTableEntry>(sessionId.ToString(), userId);
            var result = await SessionPlayersTable.ExecuteAsync(operation);
            return result.Result as NationTableEntry;
        }

        public async Task<IEnumerable<INationData>> GetNations(Guid sessionId)
        {
            List<INationData> results = new List<INationData>();
            TableQuery<NationTableEntry> query = new TableQuery<NationTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            do
            {
                var queryResults = await SessionPlayersTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task MarkPlayerCompletedPhase(Guid sessionId, string userId, Guid phaseId)
        {
            // Fetch existing entry
            var operation = TableOperation.Retrieve<NationTableEntry>(sessionId.ToString(), userId);
            var result = await SessionPlayersTable.ExecuteAsync(operation);

            // Modify entry
            NationTableEntry playerEntry = result.Result as NationTableEntry;
            playerEntry.CompletedPhase = phaseId;

            // Write entry back (fails on write conflict)
            try
            {
                TableOperation insertOperation = TableOperation.Replace(playerEntry);
                await SessionPlayersTable.ExecuteAsync(insertOperation);
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
                nationEntry.AvailableReinforcementsRaw = operation.Reinforcements;
                batchOperation.Replace(nationEntry);
            }

            // Write entry back (fails on write conflict)
            try
            {
                await SessionPlayersTable.ExecuteBatchAsync(batchOperation);
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
        public CloudTable SessionTable { get; set; }
        public CloudTable SessionPlayersTable { get; set; }
    }
}
