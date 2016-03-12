using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            repository.RegionTable.DeleteIfExists();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("RegionRepository")]
        public async Task IntegrationTestCreateRegion()
        {
            // Arrange
            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            Guid dummySessionId = new Guid("74720766-452A-40AD-8A61-FEF07E8573C9");
            Guid dummyRegionId = new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3");
            Guid dummyContinentId = new Guid("DE167712-0CE6-455C-83EA-CB2A6936F1BE");
            List<Guid> dummyConnections = new List<Guid> { new Guid("0533203F-13F2-4863-B528-17F53D279E19"), new Guid("4A9779D0-0727-4AD9-AD66-17AE9AF9BE02") };

            // Act
            await repository.CreateRegion(dummySessionId, dummyRegionId, dummyContinentId, "DummyRegion", dummyConnections);

            // Assert
            TableOperation operation = TableOperation.Retrieve<RegionTableEntry>(dummySessionId.ToString(), dummyRegionId.ToString());
            TableResult result = await repository.RegionTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(RegionTableEntry));
            RegionTableEntry resultStronglyTyped = result.Result as RegionTableEntry;
            Assert.AreEqual(dummySessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(dummyRegionId, resultStronglyTyped.RegionId);
            Assert.AreEqual(dummyContinentId, resultStronglyTyped.ContinentId);
            Assert.AreEqual("DummyRegion", resultStronglyTyped.Name);
            Assert.AreEqual(String.Empty, resultStronglyTyped.OwnerId);
            Assert.AreEqual(0, resultStronglyTyped.StoredTroopCount);
            Assert.IsTrue(resultStronglyTyped.ETag.Length > 0);
            Assert.IsTrue(resultStronglyTyped.ConnectedRegions.Contains(dummyConnections[0]));
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("RegionRepository")]
        public async Task IntegrationTestGetRegion()
        {
            // Arrange
            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            Guid dummySessionId = new Guid("74720766-452A-40AD-8A61-FEF07E8573C9");
            Guid dummyRegionId = new Guid("89B6BDF0-83B7-42F1-B216-7DFFB8D11EA2");
            Guid dummyContinentId = new Guid("DE167712-0CE6-455C-83EA-CB2A6936F1BE");
            List<Guid> dummyConnections = new List<Guid> { new Guid("0533203F-13F2-4863-B528-17F53D279E19"), new Guid("4A9779D0-0727-4AD9-AD66-17AE9AF9BE02") };
            await repository.CreateRegion(dummySessionId, dummyRegionId, dummyContinentId, "DummyRegion", dummyConnections);

            // Act
            IRegionData regionData = await repository.GetRegion(dummySessionId, dummyRegionId);

            // Assert
            Assert.IsNotNull(regionData);
            Assert.AreEqual(dummySessionId, regionData.SessionId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("RegionRepository")]
        public async Task IntegrationTestGetRegion_WithInvalidRegionId()
        {
            // Arrange
            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            Guid dummySessionId = new Guid("74720766-452A-40AD-8A61-FEF07E8573C9");
            Guid dummyRegionId = new Guid("DE167712-0CE6-455C-83EA-CB2A6936F1BE");

            // Act
            IRegionData regionData = await repository.GetRegion(dummySessionId, dummyRegionId);

            // Assert
            Assert.IsNull(regionData);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("RegionRepository")]
        public async Task IntegrationTestGetRegions()
        {
            // Arrange
            RegionRepository repository = new RegionRepository(DevelopmentStorageAccountConnectionString);
            Guid dummySessionId = new Guid("74720766-452A-40AD-8A61-FEF07E8573C9");
            Guid dummyRegionId = new Guid("CBDF6EBE-5F91-4ADF-AC30-D149D8E5F8EB");
            Guid secondDummyRegionId = new Guid("336312D8-F219-4C9B-B3FE-F4B39602E28D");
            Guid dummyContinentId = new Guid("DE167712-0CE6-455C-83EA-CB2A6936F1BE");
            await repository.CreateRegion(dummySessionId, dummyRegionId, dummyContinentId, "DummyRegion", new List<Guid>());
            await repository.CreateRegion(dummySessionId, secondDummyRegionId, dummyContinentId, "DummyRegion2", new List<Guid>());

            // Act
            IEnumerable<IRegionData> regionData = await repository.GetRegions(dummySessionId);

            // Assert
            Assert.IsNotNull(regionData);
            Assert.IsTrue(regionData.Count() >= 2, "Expected at least two regions");
            Assert.AreEqual(1, regionData.Where(region => region.RegionId == dummyRegionId).Count());
            Assert.AreEqual(1, regionData.Where(region => region.RegionId == secondDummyRegionId).Count());
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
    }
}
