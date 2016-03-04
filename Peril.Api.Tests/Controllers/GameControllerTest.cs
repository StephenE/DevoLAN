using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Controllers.Api;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Tests.Controllers
{
    [TestClass]
    public class GameControllerTest
    {
        [TestMethod]
        public async Task TestGetSessions_WithNoSessions()
        {
            // Arrange
            GameController controller = new GameController();

            // Act
            IEnumerable<ISession> result = await controller.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task TestGetSessions_WithOneSession()
        {
            // Arrange
            GameController controller = new GameController();

            // Act
            IEnumerable<ISession> result = await controller.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public async Task TestStartNewSession()
        {
            // Arrange
            GameController controller = new GameController();

            // Act
            ISession result = await controller.PostStartNewSession();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.GameId);
        }

        [TestMethod]
        public async Task TestJoinSession_WithInvalidGuid()
        {
            // Arrange
            GameController controller = new GameController();

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
        public async Task TestJoinSession_WithValidGuid()
        {
            // Arrange
            GameController controller = new GameController();

            // Act
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Task result = controller.PostJoinSession(validGuid);

            // Assert
            await result;
        }

        [TestMethod]
        public async Task TestGetPlayers_WithInvalidGuid()
        {
            // Arrange
            GameController controller = new GameController();

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
        public async Task TestGetPlayers_WithValidGuidAndOnePlayer()
        {
            // Arrange
            GameController controller = new GameController();

            // Act
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            IEnumerable<IPlayer> result = await controller.GetPlayers(validGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }
    }
}
