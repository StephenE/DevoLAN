using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Repository.Azure.Tests.Repository;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests
{
    [TestClass]
    public class SessionRepositoryIntergrationTest
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
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestCreateSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            String dummyUserId = "DummyUserId";

            // Act
            Guid newSessionGuid = await repository.CreateSession(dummyUserId, PlayerColour.Black);

            // Assert
            Assert.IsNotNull(newSessionGuid);

            TableOperation operation = TableOperation.Retrieve<SessionTableEntry>(newSessionGuid.ToString(), dummyUserId);
            TableResult result = await SessionTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(SessionTableEntry));
            SessionTableEntry resultStronglyTyped = result.Result as SessionTableEntry;
            Assert.AreEqual(dummyUserId, resultStronglyTyped.OwnerUserId);
            Assert.AreEqual(Guid.Empty, resultStronglyTyped.PhaseId);
            Assert.AreEqual(SessionPhase.NotStarted, resultStronglyTyped.PhaseType);

            operation = TableOperation.Retrieve<NationTableEntry>(newSessionGuid.ToString(), dummyUserId);
            result = await repository.SessionPlayersTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(NationTableEntry));
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(newSessionGuid, resultPlayerStronglyTyped.SessionId);
            Assert.AreEqual(dummyUserId, resultPlayerStronglyTyped.UserId);
            Assert.AreEqual(Guid.Empty, resultPlayerStronglyTyped.CompletedPhase);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestGetSessionPlayers()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68C1756F-1ED5-449A-9CD1-F533C3A539A0");
            String dummyUserId = "DummyUserId";
            await repository.SetupSession(validGuid, dummyUserId);

            // Act
            IEnumerable<IPlayer> sessionPlayers = await repository.GetSessionPlayers(validGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First().UserId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestGetSessions()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("DB709596-016C-4A56-BE4B-9F5271D2EAD2");
            String dummyUserId = "DummyUserId";
            await repository.SetupSession(validGuid, dummyUserId);

            // Act
            IEnumerable<Core.ISession> createdSessions = await repository.GetSessions();

            // Assert
            Assert.IsNotNull(createdSessions);
            Assert.AreEqual(1, createdSessions.Where(session => session.GameId == validGuid).Count());
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestGetSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("093E1A88-B598-48F5-8E7D-CCF27D47DB61");
            String dummyUserId = "DummyUserId";
            await repository.SetupSession(validGuid, dummyUserId);

            // Act
            ISession session = await repository.GetSession(validGuid);

            // Assert
            Assert.AreEqual(validGuid, session.GameId);
            Assert.AreEqual(Guid.Empty, session.PhaseId);
            Assert.AreEqual(SessionPhase.NotStarted, session.PhaseType);

            // Act
            IEnumerable<IPlayer> sessionPlayers = await repository.GetSessionPlayers(validGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First().UserId);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestGetSession_WithInvalidSessionId()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");

            // Act
            ISession result = await repository.GetSession(invalidGuid);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestReservePlayerColour()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("E5894BE3-6074-4516-93FB-BC851C1E4246");
            ISessionData sessionData = await repository.SetupSession(validGuid, "CreatingUser");

            // Act
            bool isReserved = await repository.ReservePlayerColour(validGuid, sessionData.CurrentEtag, PlayerColour.Blue);

            // Assert
            Assert.AreEqual(true, isReserved);
            TableOperation operation = TableOperation.Retrieve<SessionTableEntry>(validGuid.ToString(), "CreatingUser");
            TableResult result = await SessionTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(SessionTableEntry));
            SessionTableEntry resultStronglyTyped = result.Result as SessionTableEntry;
            Assert.IsTrue(resultStronglyTyped.IsColourUsed(PlayerColour.Blue));
            Assert.AreNotEqual(resultStronglyTyped.CurrentEtag, sessionData.CurrentEtag);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestReservePlayerColour_WithTakenColour()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("BFB365F0-25FF-40A7-84B6-4177B3FC7080");
            ISessionData sessionData = await repository.SetupSession(validGuid, "CreatingUser");
            await repository.ReservePlayerColour(validGuid, sessionData.CurrentEtag, PlayerColour.Blue);
            sessionData = await repository.GetSession(validGuid);

            // Act
            bool isReserved = await repository.ReservePlayerColour(validGuid, sessionData.CurrentEtag, PlayerColour.Blue);

            // Assert
            Assert.AreEqual(false, isReserved);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestReservePlayerColour_WithIncorrectEtag()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("BFB365F0-25FF-40A7-84B6-4177B3FC7080");
            await repository.SetupSession(validGuid, "CreatingUser");

            // Act
            Task<bool> isReserved = repository.ReservePlayerColour(validGuid, "Invalid ETAG", PlayerColour.Blue);

            // Assert
            try
            {
                await isReserved;
                Assert.Fail("Expected exception to be thrown");
            }
            catch(ConcurrencyException)
            {
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationTestJoinSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            String dummyUserId = "DummyUserId";
            await repository.SetupSession(validGuid, "CreatingUser");

            // Act
            await repository.JoinSession(validGuid, dummyUserId, PlayerColour.Black);

            // Assert
            var operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), dummyUserId);
            var result = await repository.SessionPlayersTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(NationTableEntry));
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(validGuid, resultPlayerStronglyTyped.SessionId);
            Assert.AreEqual(dummyUserId, resultPlayerStronglyTyped.UserId);
            Assert.AreEqual(Guid.Empty, resultPlayerStronglyTyped.CompletedPhase);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("SessionRepository")]
        public async Task IntegrationMarkPlayerCompletedPhase()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            String dummyUserId = "DummyUserId";
            await repository.SetupSession(validGuid, dummyUserId)
                            .SetupSessionPhase(repository, SessionPhase.Reinforcements);

            // Act
            await repository.MarkPlayerCompletedPhase(validGuid, dummyUserId, validGuid);

            // Assert
            var operation = TableOperation.Retrieve<NationTableEntry>(validGuid.ToString(), dummyUserId);
            var result = await repository.SessionPlayersTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(NationTableEntry));
            NationTableEntry resultPlayerStronglyTyped = result.Result as NationTableEntry;
            Assert.AreEqual(validGuid, resultPlayerStronglyTyped.CompletedPhase);
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
