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

        public async Task<IEnumerable<ICardData>> GetCards(Guid sessionId, String userId)
        {
            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            var rowKeyCondition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "Card_"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "Card`")
            );
            var ownerIdCondition = TableQuery.CombineFilters(
                rowKeyCondition,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("OwnerId", QueryComparisons.Equal, userId)
            );
            var ownerStateCondition = TableQuery.CombineFilters(
                ownerIdCondition,
                TableOperators.And,
                TableQuery.GenerateFilterConditionForInt("OwnerStateRaw", QueryComparisons.Equal, (Int32)CardTableEntry.State.Owned)
            );

            List <ICardData> results = new List<ICardData>();
            TableQuery<CardTableEntry> query = new TableQuery<CardTableEntry>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()),
                    TableOperators.And,
                    ownerStateCondition
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

            foreach (CardTableEntry entry in results)
            {
                entry.IsValid();
            }

            return results;
        }

        public void SetCardOwner(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid regionId, string userId, string currentEtag)
        {
            SetCardInternal(batchOperationHandleInterface, sessionId, regionId, CardTableEntry.State.Owned, userId, currentEtag);
        }

        public void SetCardDiscarded(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid regionId, string currentEtag)
        {
            SetCardInternal(batchOperationHandleInterface, sessionId, regionId, CardTableEntry.State.Discarded, String.Empty, currentEtag);
        }

        public void SetCardUnowned(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid regionId, string currentEtag)
        {
            SetCardInternal(batchOperationHandleInterface, sessionId, regionId, CardTableEntry.State.Unowned, String.Empty, currentEtag);
        }

        private void SetCardInternal(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid regionId, CardTableEntry.State newState, String newOwnerId, String currentEtag)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;

            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Create a DynamicTableEntity so that we can do a partial update of this table (a merge)
            DynamicTableEntity cardEntry = CardTableEntry.CreateDynamicTableEntity(sessionId, regionId, currentEtag);
            CardTableEntry.SetOwner(cardEntry, newState, newOwnerId);
            batchOperationHandle.BatchOperation.Merge(cardEntry);
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

        public void SetAvailableReinforcements(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, String userId, String currentEtag, UInt32 reinforcements)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;

            // Get the session data table
            CloudTable sessionDataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            // Create a DynamicTableEntity so that we can do a partial update of this table (a merge)
            DynamicTableEntity nationEntry = NationTableEntry.CreateDynamicTableEntity(sessionId, userId, currentEtag);
            NationTableEntry.SetAvailableReinforcements(nationEntry, reinforcements);
            batchOperationHandle.BatchOperation.Merge(nationEntry);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
    }
}
