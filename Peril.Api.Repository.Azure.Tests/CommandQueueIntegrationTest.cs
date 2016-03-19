using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

            RegionTable = TableClient.GetTableReference(RegionRepository.TableName);
            RegionTable.CreateIfNotExists();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("CommandQueue")]
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
        [TestCategory("CommandQueue")]
        [Ignore]
        public async Task IntegrationTestOrderAttack()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);
            Guid targetRegionGuid = new Guid("8449A25B-363D-4F01-B3D9-7EF8C5D42047");
            RegionTableEntry newRegion = new RegionTableEntry(SessionGuid, RegionGuid, RegionGuid, "Name");
            TableOperation insertOperation = TableOperation.InsertOrReplace(newRegion);
            await RegionTable.ExecuteAsync(insertOperation);

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

            var regionOperation = TableOperation.Retrieve<RegionTableEntry>(SessionGuid.ToString(), RegionGuid.ToString());
            var regionResult = await RegionTable.ExecuteAsync(regionOperation);
            RegionTableEntry regionData = regionResult.Result as RegionTableEntry;
            Assert.IsNotNull(regionData);
            Assert.AreEqual(5U, regionData.TroopsCommittedToPhase);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("CommandQueue")]
        public async Task IntegrationTestRedeploy()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);
            Guid targetRegionGuid = new Guid("8449A25B-363D-4F01-B3D9-7EF8C5D42047");

            // Act
            Guid operationGuid = await repository.Redeploy(SessionGuid, SessionPhaseGuid, String.Empty, RegionGuid, targetRegionGuid, 5U);

            // Assert
            var operation = TableOperation.Retrieve<CommandQueueTableEntry>(SessionGuid.ToString(), operationGuid.ToString());
            var result = await CommandTable.ExecuteAsync(operation);
            CommandQueueTableEntry queuedCommand = result.Result as CommandQueueTableEntry;
            Assert.IsNotNull(queuedCommand);
            Assert.AreEqual(operationGuid, queuedCommand.OperationId);
            Assert.AreEqual(SessionGuid, queuedCommand.SessionId);
            Assert.AreEqual(SessionPhaseGuid, queuedCommand.PhaseId);
            Assert.AreEqual(CommandQueueMessageType.Redeploy, queuedCommand.MessageType);
            Assert.AreEqual(RegionGuid, queuedCommand.SourceRegion);
            Assert.AreEqual(targetRegionGuid, queuedCommand.TargetRegion);
            Assert.AreEqual(5U, queuedCommand.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("CommandQueue")]
        public async Task IntegrationTestGetQueuedCommands()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);
            Guid operationGuid = await repository.DeployReinforcements(SessionGuid, SessionPhaseGuid, RegionGuid, "DummyEtag", 10U);
            Guid secondOperationGuid = await repository.DeployReinforcements(SessionGuid, SessionPhaseGuid, RegionGuid, "DummyEtag", 5U);

            // Act
            IEnumerable<ICommandQueueMessage> pendingMessages = await repository.GetQueuedCommands(SessionGuid, SessionPhaseGuid);

            // Assert
            Assert.IsNotNull(pendingMessages);
            Assert.IsTrue(2 <= pendingMessages.Count());
            foreach(IDeployReinforcementsMessage message in pendingMessages)
            {
                Assert.AreEqual(SessionGuid, message.SessionId);
                Assert.AreEqual(SessionPhaseGuid, message.PhaseId);
                if (message.OperationId == operationGuid)
                {
                    Assert.AreEqual(CommandQueueMessageType.Reinforce, message.MessageType);
                    Assert.AreEqual(10U, message.NumberOfTroops);
                }
                else if (message.OperationId == secondOperationGuid)
                {
                    Assert.AreEqual(CommandQueueMessageType.Reinforce, message.MessageType);
                    Assert.AreEqual(5U, message.NumberOfTroops);
                }
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("CommandQueue")]
        public async Task IntegrationTestRemoveCommands()
        {
            // Arrange
            CommandQueue repository = new CommandQueue(DevelopmentStorageAccountConnectionString);
            Guid sessionPhaseId = Guid.NewGuid();
            CloudTable randomCommandTable = CommandQueue.GetCommandQueueTableForSessionPhase(TableClient, sessionPhaseId);
            randomCommandTable.CreateIfNotExists();

            // Act
            await repository.RemoveCommands(sessionPhaseId);

            // Assert
            Assert.IsFalse(randomCommandTable.Exists());
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
        static private CloudTable RegionTable { get; set; }
        static private CloudTable CommandTable { get; set; }
    }
}
