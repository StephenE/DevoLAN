using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

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

        public Task AddArmyToCombat(Guid sessionId, CombatType sourceType, IDictionary<Guid, IEnumerable<ICombatArmy>> armies)
        {
            throw new NotImplementedException();
        }

        public Task AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            throw new NotImplementedException();
        }

        public Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType type)
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
