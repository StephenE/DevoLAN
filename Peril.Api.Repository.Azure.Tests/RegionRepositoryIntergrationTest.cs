using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests
{
    [TestClass]
    public class RegionRepositoryIntergrationTest
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            StorageAccount = CloudStorageAccount.Parse(DevelopmentStorageAccountConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
            SessionTable = TableClient.GetTableReference("Sessions");
            SessionPlayerTable = TableClient.GetTableReference("SessionPlayers");

            SessionTable.DeleteIfExists();
            SessionPlayerTable.DeleteIfExists();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("RegionRepository")]
        public async Task IntegrationTestGetRegion()
        {
            // Arrange
            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            Guid dummyRegionId = new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3");

            // Act
            IRegionData regionData = await repository.GetRegion(dummyRegionId);

            // Assert
            Assert.IsNotNull(regionData);
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
        static private CloudTable SessionTable { get; set; }
        static private CloudTable SessionPlayerTable { get; set; }
    }
}
