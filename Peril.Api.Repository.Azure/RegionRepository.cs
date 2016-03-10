using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class RegionRepository : IRegionRepository
    {
        public RegionRepository(String storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            RegionTable = TableClient.GetTableReference("Regions");
            RegionTable.CreateIfNotExists();
        }

        public async Task<IRegionData> GetRegion(Guid regionId)
        {
            throw new NotImplementedException("TODO");
        }

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudTableClient TableClient { get; set; }
        public CloudTable RegionTable { get; set; }
    }
}
