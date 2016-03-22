using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using System.Net;

namespace Peril.Api.Repository.Azure
{
    public class WorldRepository : IWorldRepository
    {
        public WorldRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            RandomNumberGenerator = new Random();
        }

        public async Task AddArmyToCombat(Guid sessionId, CombatType sourceType, IDictionary<Guid, IEnumerable<ICombatArmy>> armies)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Guid>> AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Insert armies
            List<Guid> createdCombatIds = new List<Guid>();
            foreach (var armyEntry in armies)
            {
                Guid combatId = Guid.NewGuid();
                CombatTableEntry entry = new CombatTableEntry(sessionId, combatId, armyEntry.Item1);
                entry.SetCombatArmy(armyEntry.Item2);
                batchOperation.Insert(entry);
                createdCombatIds.Add(combatId);
            }

            // Write entry back (fails on write conflict)
            try
            {
                CloudTable dataTable = GetTableForSessionData(sessionId, 1);
                await dataTable.ExecuteBatchAsync(batchOperation);
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

            return createdCombatIds;
        }

        public async Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            CloudTable dataTable = GetTableForSessionData(sessionId, 1);

            TableQuery<CombatTableEntry> query = new TableQuery<CombatTableEntry>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            List<ICombat> results = new List<ICombat>();
            do
            {
                var queryResults = await dataTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetRandomNumberGenerator(Guid targetRegion, int minimum, int maximum)
        {
            yield return RandomNumberGenerator.Next(minimum, maximum);
        }

        public CloudTable GetTableForSessionData(Guid sessionId, UInt32 roundNumber)
        {
            return SessionRepository.GetTableForSessionData(TableClient, sessionId, roundNumber);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        private Random RandomNumberGenerator { get; set; }
    }
}
