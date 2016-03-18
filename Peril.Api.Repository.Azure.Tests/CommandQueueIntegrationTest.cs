using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Azure.Tests.Repository;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests
{
    [TestClass]
    public class CommandQueueIntegrationTest
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            StorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            TableClient = StorageAccount.CreateCloudTableClient();

            CommandTable = CommandQueue.GetCommandQueueTableForSessionPhase(TableClient, SessionPhaseGuid);
            CommandTable.CreateIfNotExists();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestCreateReinforceMessage()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);

            // Act
            Guid operationGuid = await repository.DeployReinforcements(SessionGuid, SessionPhaseGuid, RegionGuid, "DummyEtag", 10U);

            // Assert
            var operation = TableOperation.Retrieve<CommandQueueTableEntry>(SessionGuid.ToString(), operationGuid.ToString());
            var result = await CommandTable.ExecuteAsync(operation);
            CommandQueueTableEntry queuedCommand = result.Result as CommandQueueTableEntry;
            Assert.IsNotNull(queuedCommand);
            Assert.AreEqual(operationGuid, queuedCommand.OperationId);
            Assert.AreEqual(SessionGuid, queuedCommand.SessionId);
            Assert.AreEqual(SessionPhaseGuid, queuedCommand.PhaseId);
            Assert.AreEqual(CommandQueueMessageType.Reinforce, queuedCommand.MessageType);
            Assert.AreEqual(RegionGuid, queuedCommand.TargetRegion);
            Assert.AreEqual("DummyEtag", queuedCommand.TargetRegionEtag);
            Assert.AreEqual(10U, queuedCommand.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestOrderAttack()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);
            Guid targetRegionGuid = new Guid("8449A25B-363D-4F01-B3D9-7EF8C5D42047");

            // Act
            Guid operationGuid = await repository.OrderAttack(SessionGuid, SessionPhaseGuid, RegionGuid, "DummyEtag", targetRegionGuid, 5U);

            // Assert
            var operation = TableOperation.Retrieve<CommandQueueTableEntry>(SessionGuid.ToString(), operationGuid.ToString());
            var result = await CommandTable.ExecuteAsync(operation);
            CommandQueueTableEntry queuedCommand = result.Result as CommandQueueTableEntry;
            Assert.IsNotNull(queuedCommand);
            Assert.AreEqual(operationGuid, queuedCommand.OperationId);
            Assert.AreEqual(SessionGuid, queuedCommand.SessionId);
            Assert.AreEqual(SessionPhaseGuid, queuedCommand.PhaseId);
            Assert.AreEqual(CommandQueueMessageType.Attack, queuedCommand.MessageType);
            Assert.AreEqual(RegionGuid, queuedCommand.SourceRegion);
            Assert.AreEqual("DummyEtag", queuedCommand.SourceRegionEtag);
            Assert.AreEqual(targetRegionGuid, queuedCommand.TargetRegion);
            Assert.AreEqual(5U, queuedCommand.NumberOfTroops);
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private Guid SessionGuid { get { return new Guid("058CABC4-4A9C-458E-8D6F-0DB84A01FB0B"); } }
        static private Guid SessionPhaseGuid { get { return new Guid("748C0DD2-8931-4F69-91D7-80EA13EF8A6E"); } }
        static private Guid RegionGuid { get { return new Guid("A1A425EE-F522-4BFE-8781-EDB2C2873CDB"); } }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
        static private CloudTable CommandTable { get; set; }
    }
}
