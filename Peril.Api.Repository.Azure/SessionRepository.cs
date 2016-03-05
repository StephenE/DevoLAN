using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class SessionRepository : ISessionRepository
    {
        public SessionRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            SessionTable = TableClient.GetTableReference("Sessions");
            SessionTable.CreateIfNotExists();
            SessionPlayersTable = TableClient.GetTableReference("SessionPlayers");
            SessionPlayersTable.CreateIfNotExists();
        }

        public async Task<Guid> CreateSession(String userId)
        {
            // Create a new table entry
            Guid newSessionGuid = Guid.NewGuid();
            SessionTableEntry newSession = new SessionTableEntry(newSessionGuid);
            newSession.OwnerUserId = userId;

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newSession);
            await SessionTable.ExecuteAsync(insertOperation);

            // Add the player to the session
            await JoinSession(newSessionGuid, userId);

            // Return the new session GUID
            return newSessionGuid;
        }

        public async Task<IEnumerable<String>> GetSessionPlayers(Guid session)
        {
            TableQuery query = new TableQuery()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, session.ToString()))
                .Select(new List<String> { "RowKey" });

            var queryResults = await SessionPlayersTable.ExecuteQuerySegmentedAsync(query, null);
            return queryResults.Select(element => element.RowKey);
        }

        public async Task<IEnumerable<ISession>> GetSessions()
        {
            // Doing a full query like this is very expensive, as if we ever need to support more than a couple of sessions
            // then we should probably maintain a list of open sessions in the database
            TableQuery<SessionTableEntry> query = new TableQuery<SessionTableEntry>();

            return await SessionTable.ExecuteQuerySegmentedAsync(query, null);
        }

        public async Task<ISession> GetSession(Guid sessionId)
        {
            TableOperation operation = TableOperation.Retrieve<SessionTableEntry>(sessionId.ToString(), sessionId.ToString());
            TableResult retrievedResult = await SessionTable.ExecuteAsync(operation);
            return retrievedResult.Result as ISession;
        }

        public async Task JoinSession(Guid sessionId, String userId)
        {
            // Create a new table entry
            SessionPlayerTableEntry newSessionPlayerEntry = new SessionPlayerTableEntry(sessionId, userId);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newSessionPlayerEntry);
            await SessionPlayersTable.ExecuteAsync(insertOperation);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        private CloudTable SessionTable { get; set; }
        private CloudTable SessionPlayersTable { get; set; }
    }
}
