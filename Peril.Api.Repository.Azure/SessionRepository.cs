using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public async Task<Guid> CreateSession(String userId, PlayerColour colour)
        {
            // Create a new table entry
            Guid newSessionGuid = Guid.NewGuid();
            SessionTableEntry newSession = new SessionTableEntry(userId, newSessionGuid);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newSession);
            await SessionTable.ExecuteAsync(insertOperation);

            // Add the player to the session
            await JoinSession(newSessionGuid, userId, colour);

            // Return the new session GUID
            return newSessionGuid;
        }

        public async Task<IEnumerable<IPlayer>> GetSessionPlayers(Guid session)
        {
            List<IPlayer> results = new List<IPlayer>();
            TableQuery<NationTableEntry> query = new TableQuery<NationTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, session.ToString()));

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

        public async Task<IEnumerable<ISessionData>> GetSessions()
        {
            // Doing a full query like this is very expensive, as if we ever need to support more than a couple of sessions
            // then we should probably maintain a list of open sessions in the database
            List<SessionTableEntry> results = new List<SessionTableEntry>();
            TableQuery<SessionTableEntry> query = new TableQuery<SessionTableEntry>();

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            do
            {
                var queryResults = await SessionTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task<ISessionData> GetSession(Guid sessionId)
        {
            TableQuery<SessionTableEntry> query = new TableQuery<SessionTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()));

            var results = await SessionTable.ExecuteQuerySegmentedAsync(query, null);
            return results.FirstOrDefault();
        }

        public async Task<bool> ReservePlayerColour(Guid sessionId, String sessionEtag, PlayerColour colour)
        {
            ISessionData sessionData = await GetSession(sessionId);
            SessionTableEntry session = sessionData as SessionTableEntry;

            if(!session.IsColourUsed(colour))
            {
                if(session.ETag == sessionEtag)
                {
                    try
                    {
                        session.AddUsedColour(colour);

                        // Write entry back (fails on write conflict)
                        TableOperation insertOperation = TableOperation.Replace(session);
                        await SessionTable.ExecuteAsync(insertOperation);

                        return true;
                    }
                    catch(StorageException exception)
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
                else
                {
                    throw new ConcurrencyException();
                }
            }
            else
            {
                return false;
            }
        }

        public async Task JoinSession(Guid sessionId, String userId, PlayerColour colour)
        {
            // Create a new table entry
            NationTableEntry newSessionPlayerEntry = new NationTableEntry(sessionId, userId) { ColourId = (Int32)colour };

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newSessionPlayerEntry);
            await SessionPlayersTable.ExecuteAsync(insertOperation);
        }

        public async Task MarkPlayerCompletedPhase(Guid sessionId, String userId, Guid phaseId)
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

        public async Task SetSessionPhase(Guid sessionId, Guid currentPhaseId, SessionPhase newPhase)
        {
            throw new NotImplementedException("Not implemented");
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable SessionTable { get; set; }
        public CloudTable SessionPlayersTable { get; set; }
    }
}
