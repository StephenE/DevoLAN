using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Azure.Tests.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests
{
    [TestClass]
    public class NationRepositoryIntegrationTest
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            StorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            TableClient = StorageAccount.CreateCloudTableClient();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestGetNation()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("8B1E3932-71D5-4E29-BCAF-BE67D12C3114");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);

            // Act
            INationData sessionPlayer = await repository.GetNation(validGuid, dummyUserId);

            // Assert
            Assert.IsNotNull(sessionPlayer);
            Assert.AreEqual(dummyUserId, sessionPlayer.UserId);
            Assert.AreEqual(0U, sessionPlayer.AvailableReinforcements);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestGetNation_WithInvalidUserId()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("228175B0-CB34-453E-AB26-A76984B2FDAF");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);

            // Act
            INationData sessionPlayer = await repository.GetNation(validGuid, "InvalidUserId");

            // Assert
            Assert.IsNull(sessionPlayer);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestGetNations()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68C1756F-1ED5-449A-9CD1-F533C3A539A0");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);

            // Act
            IEnumerable<INationData> sessionPlayers = await repository.GetNations(validGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First().UserId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestGetNationsWithPopulatedTable()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("3CC9F4E8-BDB9-49F4-B128-268F0E5E9C20");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            await sessionRepository.SetupAddRegion(validGuid, Guid.NewGuid(), Guid.NewGuid(), "DummyRegion");

            // Act
            IEnumerable<INationData> sessionPlayers = await repository.GetNations(validGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First().UserId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestMarkPlayerCompletedPhase()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId)
                            .SetupSessionPhase(sessionRepository, SessionPhase.Reinforcements);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient ,validGuid);

            // Act
            await repository.MarkPlayerCompletedPhase(validGuid, dummyUserId, validGuid);

            // Assert
            var operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), "Nation_" + dummyUserId);
            var result = await dataTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(NationTableEntry));
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(validGuid, resultPlayerStronglyTyped.CompletedPhase);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestSetAvailableReinforcements()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("56C9FF34-E5DC-4CD4-B0A1-C00171D3C663");
            String dummyUserId = "DummyUserId";
            String secondDummyUserId = "DummyUserId2";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            await sessionRepository.SetupAddPlayer(validGuid, secondDummyUserId);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient, validGuid);

            // Act
            using (IBatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(validGuid)))
            {
                repository.SetAvailableReinforcements(batchOperation, validGuid, dummyUserId, "*", 10U);
            }

            // Assert
            var operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), "Nation_" + dummyUserId);
            var result = await dataTable.ExecuteAsync(operation);
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(10U, resultPlayerStronglyTyped.AvailableReinforcements);

            operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), "Nation_" + secondDummyUserId);
            result = await dataTable.ExecuteAsync(operation);
            resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(0U, resultPlayerStronglyTyped.AvailableReinforcements);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestSetAvailableReinforcements_WithMultipleUsers()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("B59C5710-F3B3-4AAB-983D-9899ADEB4F28");
            String dummyUserId = "DummyUserId";
            String secondDummyUserId = "DummyUserId2";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            await sessionRepository.SetupAddPlayer(validGuid, secondDummyUserId);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient, validGuid);

            // Act
            using (IBatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(validGuid)))
            {
                repository.SetAvailableReinforcements(batchOperation, validGuid, dummyUserId, "*", 10U);
                repository.SetAvailableReinforcements(batchOperation, validGuid, secondDummyUserId, "*", 20U);
            }

            // Assert
            var operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), "Nation_" + dummyUserId);
            var result = await dataTable.ExecuteAsync(operation);
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(10U, resultPlayerStronglyTyped.AvailableReinforcements);

            operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), "Nation_" + secondDummyUserId);
            result = await dataTable.ExecuteAsync(operation);
            resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(20U, resultPlayerStronglyTyped.AvailableReinforcements);
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
    }
}
