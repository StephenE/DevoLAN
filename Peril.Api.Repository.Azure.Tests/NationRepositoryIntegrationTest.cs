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
            await sessionRepository.SetupAddRegion(validGuid, Guid.NewGuid(), Guid.NewGuid(), "DummyRegion", 3U);

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
        public async Task IntegrationTestGetCards()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("03ECAE31-CB33-4537-9E22-FE1A68BFFA08");
            Guid dummyRegionAGuid = new Guid("FD28529F-011D-42F5-B9B2-0F7AEA80CB8A");
            Guid dummyRegionBGuid = new Guid("A05EBB51-1BAA-4E4E-B43E-7E30EB89F5C7");
            Guid dummyRegionCGuid = new Guid("21B47511-8376-44F1-835D-41FF3BD1A860");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            CardTableEntry ownedCard = await sessionRepository.SetupAddCard(validGuid, dummyRegionAGuid, CardTableEntry.State.Owned, dummyUserId, 3);
            await sessionRepository.SetupAddCard(validGuid, dummyRegionBGuid, CardTableEntry.State.Unowned, String.Empty, 5);
            await sessionRepository.SetupAddCard(validGuid, dummyRegionCGuid, CardTableEntry.State.Discarded, dummyUserId, 7);

            // Act
            IEnumerable<ICardData> ownedCards = await repository.GetCards(validGuid, dummyUserId);

            // Assert
            Assert.IsNotNull(ownedCards);
            Assert.AreEqual(1, ownedCards.Count());
            Assert.AreEqual(dummyUserId, ownedCards.First().OwnerId);
            Assert.AreEqual(dummyRegionAGuid, ownedCards.First().RegionId);
            Assert.AreEqual(3U, ownedCards.First().Value);
            Assert.AreEqual(ownedCard.ETag, ownedCards.First().CurrentEtag);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestGetUnownedCards()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("03ECAE31-CB33-4537-9E22-FE1A68BFFA08");
            Guid dummyRegionAGuid = new Guid("FD28529F-011D-42F5-B9B2-0F7AEA80CB8A");
            Guid dummyRegionBGuid = new Guid("A05EBB51-1BAA-4E4E-B43E-7E30EB89F5C7");
            Guid dummyRegionCGuid = new Guid("21B47511-8376-44F1-835D-41FF3BD1A860");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            await sessionRepository.SetupAddCard(validGuid, dummyRegionAGuid, CardTableEntry.State.Owned, dummyUserId, 3);
            var unownedCard = await sessionRepository.SetupAddCard(validGuid, dummyRegionBGuid, CardTableEntry.State.Unowned, String.Empty, 5);
            await sessionRepository.SetupAddCard(validGuid, dummyRegionCGuid, CardTableEntry.State.Discarded, dummyUserId, 7);

            // Act
            IEnumerable<ICardData> unownedCards = await repository.GetUnownedCards(validGuid);

            // Assert
            Assert.IsNotNull(unownedCards);
            Assert.AreEqual(1, unownedCards.Count());
            Assert.AreEqual(String.Empty, unownedCards.First().OwnerId);
            Assert.AreEqual(dummyRegionBGuid, unownedCards.First().RegionId);
            Assert.AreEqual(5U, unownedCards.First().Value);
            Assert.AreEqual(unownedCard.ETag, unownedCards.First().CurrentEtag);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestSetCardOwner()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("745C4DEB-2C66-4AC5-900E-27BA0C82BB0F");
            Guid dummyRegionGuid = new Guid("8C60947F-F7E5-4153-B43C-4B19D8CBE2CF");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            CardTableEntry ownedCard = await sessionRepository.SetupAddCard(validGuid, dummyRegionGuid, CardTableEntry.State.Unowned, String.Empty, 3);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient, validGuid);

            // Act
            using (IBatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(validGuid)))
            {
                repository.SetCardOwner(batchOperation, validGuid, dummyRegionGuid, dummyUserId, ownedCard.CurrentEtag);
            }

            // Assert
            var operation = TableOperation.Retrieve<CardTableEntry>(validGuid.ToString(), "Card_" + dummyRegionGuid);
            var result = await dataTable.ExecuteAsync(operation);
            CardTableEntry resultStronglyTyped = result.Result as CardTableEntry;
            Assert.AreEqual(CardTableEntry.State.Owned, resultStronglyTyped.OwnerState);
            Assert.AreEqual(dummyUserId, resultStronglyTyped.OwnerId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestSetCardDiscarded()
        {
            // CHANGE GUIDS
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("C6EA373B-63E8-481D-894C-9051F6710771");
            Guid dummyRegionGuid = new Guid("7D194EC9-59ED-494F-8F77-9ED4B26F75FA");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            CardTableEntry ownedCard = await sessionRepository.SetupAddCard(validGuid, dummyRegionGuid, CardTableEntry.State.Owned, dummyUserId, 3);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient, validGuid);

            // Act
            using (IBatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(validGuid)))
            {
                repository.SetCardDiscarded(batchOperation, validGuid, dummyRegionGuid, ownedCard.CurrentEtag);
            }

            // Assert
            var operation = TableOperation.Retrieve<CardTableEntry>(validGuid.ToString(), "Card_" + dummyRegionGuid);
            var result = await dataTable.ExecuteAsync(operation);
            CardTableEntry resultStronglyTyped = result.Result as CardTableEntry;
            Assert.AreEqual(CardTableEntry.State.Discarded, resultStronglyTyped.OwnerState);
            Assert.AreEqual(dummyUserId, resultStronglyTyped.OwnerId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("NationRepository")]
        public async Task IntegrationTestResetDiscardedCards()
        {
            // Arrange
            NationRepository repository = new NationRepository(DevelopmentStorageAccountConnectionString);
            SessionRepository sessionRepository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("0FC802DC-2F1B-4D0B-B1B4-E6BD88E5550D");
            Guid dummyRegionGuid = new Guid("EAB48F5F-F268-45BE-B42D-936DE39CDD09");
            String dummyUserId = "DummyUserId";
            await sessionRepository.SetupSession(validGuid, dummyUserId);
            CardTableEntry ownedCard = await sessionRepository.SetupAddCard(validGuid, dummyRegionGuid, CardTableEntry.State.Discarded, dummyUserId, 3);
            var dataTable = SessionRepository.GetTableForSessionData(TableClient, validGuid);

            // Act
            using (IBatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(validGuid)))
            {
                await repository.ResetDiscardedCards(batchOperation, validGuid);
            }

            // Assert
            var operation = TableOperation.Retrieve<CardTableEntry>(validGuid.ToString(), "Card_" + dummyRegionGuid);
            var result = await dataTable.ExecuteAsync(operation);
            CardTableEntry resultStronglyTyped = result.Result as CardTableEntry;
            Assert.AreEqual(CardTableEntry.State.Unowned, resultStronglyTyped.OwnerState);
            Assert.AreEqual(String.Empty, resultStronglyTyped.OwnerId);
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
