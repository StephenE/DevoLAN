using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            SessionPlayersTable = TableClient.GetTableReference(NationRepository.NationTableName);
            SessionPlayersTable.CreateIfNotExists();
        }

        public async Task<Guid> CreateSession(String userId, PlayerColour colour)
        {
            Guid newSessionGuid = Guid.NewGuid();

            // Create a new table to store all the data for this session
            var dataTable = GetTableForSessionData(newSessionGuid, 1);
            await dataTable.CreateIfNotExistsAsync();

            // Create a new table entry
            SessionTableEntry newSession = new SessionTableEntry(userId, newSessionGuid);

            // Kick off the insert operation
            TableOperation insertOperation = TableOperation.Insert(newSession);
            await SessionTable.ExecuteAsync(insertOperation);

            // Add the player to the session
            await JoinSession(newSessionGuid, userId, colour);

            // Return the new session GUID
            return newSessionGuid;
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

        public async Task SetSessionPhase(Guid sessionId, Guid currentPhaseId, SessionPhase newPhase)
        {
            ISessionData sessionData = await GetSession(sessionId);
            SessionTableEntry session = sessionData as SessionTableEntry;

            if (session.PhaseId == currentPhaseId)
            {
                try
                {
                    session.PhaseId = Guid.NewGuid();
                    session.RawPhaseType = (Int32)newPhase;

                    // Create a new command queue for this session phase
                    if(CommandQueue.IsCommandQueueRequiredForPhase(newPhase))
                    {
                        CloudTable commandQueueTable = CommandQueue.GetCommandQueueTableForSessionPhase(TableClient, session.PhaseId);
                        await commandQueueTable.CreateIfNotExistsAsync();
                    }

                    // Write entry back (fails on write conflict)
                    TableOperation insertOperation = TableOperation.Replace(session);
                    await SessionTable.ExecuteAsync(insertOperation);
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
            else
            {
                throw new ConcurrencyException();
            }
        }

        public CloudTable GetTableForSessionData(Guid sessionId, UInt32 roundNumber)
        {
            return GetTableForSessionData(TableClient, sessionId, roundNumber);
        }

        static public CloudTable GetTableForSessionData(CloudTableClient tableClient, Guid sessionId, UInt32 roundNumber)
        {
            String tableName = "Data" + sessionId.ToString().Replace("-", String.Empty) + "Round" + roundNumber;
            CloudTable table = tableClient.GetTableReference(tableName);
            return table;
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable SessionTable { get; set; }
        public CloudTable SessionPlayersTable { get; set; }
    }
}
