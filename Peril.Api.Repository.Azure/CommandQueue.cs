using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class CommandQueue : ICommandQueue
    {
        static public String CommandQueueTableNameSyntax { get { return "CommandQueue{0}"; } }

        public CommandQueue(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            
        }

        public async Task<Guid> DeployReinforcements(Guid sessionId, Guid phaseId, Guid targetRegion, String targetRegionEtag, UInt32 numberOfTroops)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateReinforceMessage(sessionId, phaseId, targetRegion, targetRegionEtag, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<Guid> OrderAttack(Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateAttackMessage(sessionId, phaseId, sourceRegion, sourceRegionEtag, targetRegion, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<Guid> Redeploy(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateRedeployMessage(sessionId, phaseId, String.Empty, sourceRegion, targetRegion, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<IEnumerable<ICommandQueueMessage>> GetQueuedCommands(Guid sessionId, Guid sessionPhaseId)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            var rowKeyCondition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "Command_"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "Command`")
            );

            List<CommandQueueTableEntry> results = new List<CommandQueueTableEntry>();
            TableQuery<CommandQueueTableEntry> query = new TableQuery<CommandQueueTableEntry>()
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
                var queryResults = await commandQueueTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task RemoveCommands(Guid sessionId, IEnumerable<ICommandQueueMessage> commands)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var command in commands)
            {
                CommandQueueTableEntry commandQueueEntry = command as CommandQueueTableEntry;
                commandQueueEntry.IsValid();
                batchOperation.Delete(commandQueueEntry);
            }

            if (batchOperation.Count > 0)
            {
                // Write entry back (fails on write conflict)
                try
                {
                    await commandQueueTable.ExecuteBatchAsync(batchOperation);
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
        }

        public CloudTable GetCommandQueueTableForSession(Guid sessionId)
        {
            return GetCommandQueueTableForSession(TableClient, sessionId);
        }

        static public CloudTable GetCommandQueueTableForSession(CloudTableClient tableClient, Guid sessionId)
        {
            CloudTable commandQueueTable = SessionRepository.GetTableForSessionData(tableClient, sessionId);
            return commandQueueTable;
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
    }
}
