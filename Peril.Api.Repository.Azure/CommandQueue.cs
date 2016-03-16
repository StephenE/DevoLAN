using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class CommandQueue : ICommandQueue
    {
        static public String CommandQueueTableName { get { return "CommandQueue"; } }

        public CommandQueue(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            CommandQueueTable = TableClient.GetTableReference(CommandQueueTableName);
            CommandQueueTable.CreateIfNotExists();
        }

        public Task<Guid> DeployReinforcements(Guid sessionId, Guid phaseId, Guid targetRegion, string targetRegionEtag, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ICommandQueueMessage>> GetQueuedCommands(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> OrderAttack(Guid sessionId, Guid phaseId, Guid sourceRegion, string sourceRegionEtag, Guid targetRegion, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> Redeploy(Guid sessionId, Guid phaseId, string nationEtag, Guid sourceRegion, Guid targetRegion, uint numberOfTroops)
        {
            throw new NotImplementedException();
        }

        public Task RemoveCommands(Guid sessionId, IEnumerable<Guid> operationIds)
        {
            throw new NotImplementedException();
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable CommandQueueTable { get; set; }
    }
}
