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
    public class WorldRepositoryIntegrationTest
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            StorageAccount = CloudStorageAccount.Parse(DevelopmentStorageAccountConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddArmyToCombat()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombat()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombatResults()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombat()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombatByType()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetRandomNumberGenerator()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);

            // Act
            var randomNumbers = repository.GetRandomNumberGenerator(Guid.Empty, 1, 6).Take(100000);

            // Assert 
            Assert.AreEqual(0, randomNumbers.Count(number => number < 1));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 1));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 2));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 3));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 4));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 5));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 6));
            Assert.AreEqual(0, randomNumbers.Count(number => number > 6));
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
    }
}
