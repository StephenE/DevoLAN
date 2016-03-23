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
            // Do a query to grab all combat
            var allCombat = await GetCombat(sessionId);
            var combatByTargetRegionQuery = from combat in allCombat
                                            where combat.ResolutionType != CombatType.BorderClash
                                            from army in combat.InvolvedArmies
                                            where army.ArmyMode == CombatArmyMode.Defending
                                            let defendingRegion = army.OriginRegionId
                                            group combat by defendingRegion into combatByTargetRegion
                                            select combatByTargetRegion;
            Dictionary<Guid, IEnumerable<CombatTableEntry>> targetRegionToCombatLookup = combatByTargetRegionQuery.ToDictionary(
                entry => entry.Key,
                entry => entry.Select(combat => combat as CombatTableEntry)
            );

            // Iterate changes and apply to target regions
            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var combatEntry in armies)
            {
                Guid targetRegionId = combatEntry.Key;
                if (targetRegionToCombatLookup.ContainsKey(targetRegionId))
                {
                    foreach (CombatTableEntry combat in targetRegionToCombatLookup[targetRegionId])
                    {
                        if (sourceType < combat.ResolutionType)
                        {
                            List<ICombatArmy> existingArmies = combat.InvolvedArmies.ToList();
                            foreach (ICombatArmy army in combatEntry.Value)
                            {
                                existingArmies.Add(new CombatArmy(army.OriginRegionId, army.OwnerUserId, army.ArmyMode, army.NumberOfTroops));
                            }
                            combat.SetCombatArmy(existingArmies);

                            batchOperation.Replace(combat);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unable to find the target region in the combat lookup");
                }
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
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Insert results
            foreach (ICombatResult result in results)
            {
                CombatResultTableEntry entry = new CombatResultTableEntry(sessionId, result.CombatId, result.Rounds);
                batchOperation.Insert(entry);
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
            CloudTable dataTable = GetTableForSessionData(sessionId, 1);

            TableQuery<CombatTableEntry> query = new TableQuery<CombatTableEntry>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForInt("ResolutionTypeRaw", QueryComparisons.Equal, (Int32)type)
                ));

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
