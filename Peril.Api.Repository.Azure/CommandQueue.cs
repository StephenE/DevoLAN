using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
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

        public Task<Guid> OrderAttack(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);
            Guid operationId = Guid.NewGuid();

            TableOperation operation = TableOperation.Retrieve<CommandQueueTableEntry>(sessionId.ToString(), "Command_" + sourceRegion.ToString() + "_" + targetRegion.ToString());
            var orderAttackTask = commandQueueTable.ExecuteAsync(operation)
                .ContinueWith(getExistingTask =>
                {
                    TableResult getExistingResult = getExistingTask.Result;
                    if (getExistingResult.Result != null)
                    {
                        // Update existing entry
                        CommandQueueTableEntry existingCommand = getExistingResult.Result as CommandQueueTableEntry;
                        existingCommand.OperationId = operationId;
                        existingCommand.RawNumberOfTroops += (int)numberOfTroops;

                        // Kick off update operation
                        batchOperationHandle.BatchOperation.Replace(existingCommand);
                    }
                    else
                    {
                        // Create a new table entry
                        CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateAttackMessage(operationId, sessionId, phaseId, sourceRegion, sourceRegionEtag, targetRegion, numberOfTroops);

                        // Kick off the insert operation
                        batchOperationHandle.BatchOperation.Insert(newCommand);
                    }
                });

            batchOperationHandle.AddPrerequisite(orderAttackTask, 1);

            return Task.FromResult(operationId);
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

        public void RemoveCommand(IBatchOperationHandle batchOperationHandle, Guid sessionId, ICommandQueueMessage command)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            lock (batchOperationHandle)
            {
                TableBatchOperation batchOperation = (batchOperationHandle as BatchOperationHandle).BatchOperation;
                CommandQueueTableEntry commandQueueEntry = command as CommandQueueTableEntry;
                commandQueueEntry.IsValid();
                batchOperation.Delete(commandQueueEntry);
            }
        }

        public void RemoveCommands(IBatchOperationHandle batchOperationHandle, Guid sessionId, IEnumerable<ICommandQueueMessage> commands)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSession(sessionId);

            lock (batchOperationHandle)
            {
                foreach (var command in commands)
                {
                    RemoveCommand(batchOperationHandle, sessionId, command);
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
