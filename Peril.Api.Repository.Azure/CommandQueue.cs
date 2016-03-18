using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;

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
            CloudTable commandQueueTable = GetCommandQueueTableForSessionPhase(phaseId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateReinforceMessage(sessionId, phaseId, targetRegion, targetRegionEtag, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<Guid> OrderAttack(Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSessionPhase(phaseId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateAttackMessage(sessionId, phaseId, sourceRegion, sourceRegionEtag, targetRegion, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<Guid> Redeploy(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSessionPhase(phaseId);

            // Create a new table entry
            CommandQueueTableEntry newCommand = CommandQueueTableEntry.CreateRedeployMessage(sessionId, phaseId, String.Empty, sourceRegion, targetRegion, numberOfTroops);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newCommand);
            await commandQueueTable.ExecuteAsync(insertOperation);

            return newCommand.OperationId;
        }

        public async Task<IEnumerable<ICommandQueueMessage>> GetQueuedCommands(Guid sessionId, Guid sessionPhaseId)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSessionPhase(sessionPhaseId);

            List<CommandQueueTableEntry> results = new List<CommandQueueTableEntry>();
            TableQuery<CommandQueueTableEntry> query = new TableQuery<CommandQueueTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()));

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

        public async Task RemoveCommands(Guid sessionPhaseId)
        {
            CloudTable commandQueueTable = GetCommandQueueTableForSessionPhase(sessionPhaseId);
            await commandQueueTable.DeleteAsync();
        }

        public CloudTable GetCommandQueueTableForSessionPhase(Guid sessionPhaseId)
        {
            return GetCommandQueueTableForSessionPhase(TableClient, sessionPhaseId);
        }

        static public CloudTable GetCommandQueueTableForSessionPhase(CloudTableClient tableClient, Guid sessionPhaseId)
        {
            String commandQueueTableName = String.Format(CommandQueueTableNameSyntax, sessionPhaseId.ToString().Replace("-", String.Empty));
            CloudTable commandQueueTable = tableClient.GetTableReference(commandQueueTableName);
            return commandQueueTable;
        }

        static public bool IsCommandQueueRequiredForPhase(SessionPhase phase)
        {
            switch(phase)
            {
                case SessionPhase.Reinforcements:
                case SessionPhase.CombatOrders:
                case SessionPhase.Redeployment:
                    return true;
                default:
                    return false;
            }
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
    }
}
