using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
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
        public async Task IntegrationTestCreateSession_ExpectInGetSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            String dummyUserId = "DummyUserId";

            // Act
            Guid newSessionGuid = await repository.CreateSession(dummyUserId);

            // Assert
            Assert.IsNotNull(newSessionGuid);
            TableOperation operation = TableOperation.Retrieve<SessionTableEntry>(newSessionGuid.ToString(), newSessionGuid.ToString());
            TableResult result = await SessionTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(SessionTableEntry));
            SessionTableEntry resultStronglyTyped = result.Result as SessionTableEntry;
            Assert.AreEqual(dummyUserId, resultStronglyTyped.OwnerUserId);

            // Act
            IEnumerable<Core.ISession> createdSessions = await repository.GetSessions();

            // Assert
            Assert.IsNotNull(createdSessions);
            Assert.IsNotNull(createdSessions.Where(session => session.GameId == newSessionGuid));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IntegrationTestCreateSession_ExpectCreatingPlayerInSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            String dummyUserId = "DummyUserId";

            // Act
            Guid newSessionGuid = await repository.CreateSession(dummyUserId);

            // Assert
            Assert.IsNotNull(newSessionGuid);

            // Act
            ISession session = await repository.GetSession(newSessionGuid);

            // Assert
            Assert.AreEqual(newSessionGuid, session.GameId);
            Assert.AreEqual(Guid.Empty, session.PhaseId);
            Assert.AreEqual(SessionPhase.NotStarted, session.PhaseType);

            // Act
            IEnumerable<String> sessionPlayers = await repository.GetSessionPlayers(newSessionGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First());
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IntegrationTestJoinSession_ExpectPlayerInSession()
        {
            // Arrange
            SessionRepository repository = new SessionRepository(DevelopmentStorageAccountConnectionString);
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            String dummyUserId = "DummyUserId";
            SessionTableEntry newSession = new SessionTableEntry(validGuid);
            TableOperation insertOperation = TableOperation.InsertOrReplace(newSession);
            await SessionTable.ExecuteAsync(insertOperation);

            // Act
            await repository.JoinSession(validGuid, dummyUserId);
            IEnumerable<String> sessionPlayers = await repository.GetSessionPlayers(validGuid);

            // Assert
            Assert.IsNotNull(sessionPlayers);
            Assert.AreEqual(1, sessionPlayers.Count());
            Assert.AreEqual(dummyUserId, sessionPlayers.First());
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
