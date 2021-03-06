﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

        public void AddArmyToCombat(IBatchOperationHandle batchOperationHandleInterface, ICombat combat, IEnumerable<ICombatArmy> armies)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;
            CombatTableEntry combatEntry = combat as CombatTableEntry;
            combatEntry.IsValid();

            List<ICombatArmy> existingArmies = combat.InvolvedArmies.ToList();
            foreach (var army in armies)
            {
                existingArmies.Add(new CombatArmy(army.OriginRegionId, army.OwnerUserId, army.ArmyMode, army.NumberOfTroops));
            }
            combatEntry.SetCombatArmy(existingArmies);

            batchOperationHandle.BatchOperation.Replace(combatEntry);
        }

        public void AddCombat(IBatchOperationHandle batchOperationHandleInterface, Guid sessionId, UInt32 round, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            BatchOperationHandle batchOperationHandle = batchOperationHandleInterface as BatchOperationHandle;

            // Insert armies
            List<Guid> createdCombatIds = new List<Guid>();
            foreach (var armyEntry in armies)
            {
                Guid combatId = Guid.NewGuid();
                CombatTableEntry entry = new CombatTableEntry(sessionId, round, combatId, armyEntry.Item1);
                entry.SetCombatArmy(armyEntry.Item2);
                batchOperationHandle.BatchOperation.Insert(entry);
                createdCombatIds.Add(combatId);
            }
        }

        public async Task AddCombatResults(Guid sessionId, UInt32 round, IEnumerable<ICombatResult> results)
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
                CloudTable dataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);
                await dataTable.CreateIfNotExistsAsync();
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

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, UInt32 round)
        {
            CloudTable dataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            TableQuery<CombatTableEntry> query = new TableQuery<CombatTableEntry>()
                .Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForInt("Round", QueryComparisons.Equal, (Int32)round)
                ));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            List<CombatTableEntry> results = new List<CombatTableEntry>();
            do
            {
                var queryResults = await dataTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            foreach(CombatTableEntry combat in results)
            {
                combat.IsValid();
            }

            return results;
        }

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, UInt32 round, CombatType type)
        {
            CloudTable dataTable = SessionRepository.GetTableForSessionData(TableClient, sessionId);

            TableQuery<CombatTableEntry> query = new TableQuery<CombatTableEntry>()
                .Where(
                TableQuery.CombineFilters(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sessionId.ToString()),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForInt("ResolutionTypeRaw", QueryComparisons.Equal, (Int32)type)
                    ),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForInt("Round", QueryComparisons.Equal, (Int32)round)
                ));

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            // Loop until the continuation token comes back as null
            List<CombatTableEntry> results = new List<CombatTableEntry>();
            do
            {
                var queryResults = await dataTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            foreach (CombatTableEntry combat in results)
            {
                combat.IsValid();
            }

            return results;
        }

        public IEnumerable<int> GetRandomNumberGenerator(Guid targetRegion, int minimum, int maximum)
        {
            yield return RandomNumberGenerator.Next(minimum, maximum);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        private Random RandomNumberGenerator { get; set; }
    }
}
