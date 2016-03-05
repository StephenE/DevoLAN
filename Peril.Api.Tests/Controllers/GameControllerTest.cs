using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Controllers.Api;
using Peril.Api.Tests.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Tests.Controllers
{
    [TestClass]
    public class GameControllerTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestGetSessions_WithNoSessions()
        {
            // Arrange
            GameController controller = CreateGameControllerWithDummySessionRepository();

            // Act
            IEnumerable<ISession> result = await controller.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestGetSessions_WithOneSession()
        {
            // Arrange
            DummySessionRepository repository = new DummySessionRepository();
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            repository.Sessions.Add(new DummySession { GameId = validGuid });
            GameController controller = CreateGameControllerWithDummySessionRepository(repository);

            // Act
            IEnumerable<ISession> result = await controller.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(validGuid, result.First().GameId);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestStartNewSession()
        {
            // Arrange
            DummySessionRepository repository = new DummySessionRepository();
            GameController controller = CreateGameControllerWithDummySessionRepository(repository);

            // Act
            ISession result = await controller.PostStartNewSession();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.GameId);
            Assert.AreEqual("DummyUser", repository.Sessions.Where(session => session.GameId == result.GameId).First().Players.First());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestJoinSession_WithInvalidGuid()
        {
            // Arrange
            GameController controller = CreateGameControllerWithDummySessionRepository();

            // Act
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            Task result = controller.PostJoinSession(invalidGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch(HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestJoinSession_WithValidGuid()
        {
            // Arrange
            DummySessionRepository repository = new DummySessionRepository();
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            repository.Sessions.Add(new DummySession { GameId = validGuid });
            GameController controller = CreateGameControllerWithDummySessionRepository(repository);

            // Act
            Task result = controller.PostJoinSession(validGuid);

            // Assert
            await result;
            Assert.AreEqual("DummyUser", repository.Sessions.Where(session => session.GameId == validGuid).First().Players.First());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestJoinSession_WithValidGuid_WithAlreadyInSession()
        {
            // Arrange
            DummySessionRepository repository = new DummySessionRepository();
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            repository.Sessions.Add(new DummySession { GameId = validGuid, Players = new List<String> { "DummyUser" } });
            GameController controller = CreateGameControllerWithDummySessionRepository(repository);

            // Act
            Task result = controller.PostJoinSession(validGuid);

            // Assert
            await result;
            Assert.AreEqual(1, repository.Sessions.Where(session => session.GameId == validGuid).First().Players.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestGetPlayers_WithInvalidGuid()
        {
            // Arrange
            GameController controller = CreateGameControllerWithDummySessionRepository();

            // Act
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            Task<IEnumerable<IPlayer>> result = controller.GetPlayers(invalidGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task TestGetPlayers_WithValidGuidAndOnePlayer()
        {
            // Arrange
            DummySessionRepository repository = new DummySessionRepository();
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            repository.Sessions.Add(new DummySession { GameId = validGuid, Players = new List<String> { "DummyUser" } });
            GameController controller = CreateGameControllerWithDummySessionRepository(repository);

            // Act
            IEnumerable<IPlayer> result = await controller.GetPlayers(validGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("DummyUser", result.First().UserId);
        }

        GameController CreateGameControllerWithDummySessionRepository()
        {
            return CreateGameControllerWithDummySessionRepository(new DummySessionRepository());
        }

        GameController CreateGameControllerWithDummySessionRepository(DummySessionRepository repository)
        {
            GameController controller = new GameController(repository, new DummyUserRepository());
            GenericIdentity identity = new GenericIdentity("DummyUser", "Dummy");
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "DummyUser"));
            controller.ControllerContext.RequestContext.Principal = new GenericPrincipal(identity, null);
            return controller;
        }
    }
}
