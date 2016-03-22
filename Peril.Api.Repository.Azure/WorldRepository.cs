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

        public async Task AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Insert armies
            foreach (var armyEntry in armies)
            {
                Guid combatId = Guid.NewGuid();
                CombatTableEntry entry = new CombatTableEntry(sessionId, combatId, armyEntry.Item1);
                entry.SetCombatArmy(armyEntry.Item2);
                batchOperation.Insert(entry);
            }

            // Write entry back (fails on write conflict)
            try
            {
                CloudTable dataTable = GetTableForSessionData(sessionId);
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

        public async Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetRandomNumberGenerator(Guid targetRegion, int minimum, int maximum)
        {
            yield return RandomNumberGenerator.Next(minimum, maximum);
        }

        public CloudTable GetTableForSessionData(Guid sessionId)
        {
            return SessionRepository.GetTableForSessionData(TableClient, sessionId);
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        private Random RandomNumberGenerator { get; set; }
    }
}
