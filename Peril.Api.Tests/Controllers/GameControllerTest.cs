using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Tests.Repository;
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
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestGetSessions_WithNoSessions()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            IEnumerable<ISession> result = await primaryUser.GameController.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestGetSessions_WithOneSession()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid);

            // Act
            IEnumerable<ISession> result = await primaryUser.GameController.GetSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(validGuid, result.First().GameId);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        [DeploymentItem(@"Data\ValidWorldDefinition.xml", "WorldData")]
        public async Task TestStartNewSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.RegionRepository.WorldDefinitionPath = @"WorldData\ValidWorldDefinition.xml";

            // Act
            ISession result = await primaryUser.GameController.PostStartNewSession(PlayerColour.Black);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.GameId);
            Assert.AreEqual(Guid.Empty, result.PhaseId);
            Assert.AreEqual(SessionPhase.NotStarted, result.PhaseType);
            Assert.AreEqual("DummyUser", primaryUser.SessionRepository.Sessions.Where(session => session.GameId == result.GameId).First().OwnerId);
            Assert.AreEqual("DummyUser", primaryUser.SessionRepository.Sessions.Where(session => session.GameId == result.GameId).First().Players.First().UserId);
            Assert.AreEqual(PlayerColour.Black, primaryUser.SessionRepository.Sessions.Where(session => session.GameId == result.GameId).First().Players.First().Colour);
            Assert.AreEqual(6, primaryUser.RegionRepository.RegionData.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithInvalidGuid()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            Task result = primaryUser.GameController.PostJoinSession(invalidGuid, PlayerColour.Black);

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
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithValidGuid()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Blue);

            // Assert
            await result;
            Assert.AreEqual(2, primaryUser.SessionRepository.Sessions.Where(session => session.GameId == validGuid).First().Players.Count);
            Assert.AreEqual(1, primaryUser.SessionRepository.Sessions.Where(session => session.GameId == validGuid).First().Players.Where(player => player.UserId == "DummyUser").Count());
            Assert.AreEqual(PlayerColour.Blue, primaryUser.SessionRepository.Sessions.Where(session => session.GameId == validGuid).First().Players.Where(player => player.UserId == "DummyUser").First().Colour);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithAlreadyInSession()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid);

            // Act
            Task result = primaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Blue);

            // Assert
            await result;
            Assert.AreEqual(1, primaryUser.SessionRepository.Sessions.Where(session => session.GameId == validGuid).First().Players.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithDuplicateColour()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Black);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.NotAcceptable, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithConcurrentOperation()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            ControllerMock secondaryUser = new ControllerMock(DummyUserRepository.RegisteredUserIds[1], primaryUser);
            primaryUser.SetupDummySession(validGuid, DummyUserRepository.RegisteredUserIds[2]);
            TaskCompletionSource<bool> blockConcurrentOperations = new TaskCompletionSource<bool>();
            primaryUser.SessionRepository.StorageDelaySimulationTask = blockConcurrentOperations.Task;

            // Act
            Task result = primaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Blue);
            Task secondResult = secondaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Blue);
            blockConcurrentOperations.SetResult(true);

            // Assert
            try
            {
                await result;
                await secondResult;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestJoinSession_WithValidGuid_WithSessionStarted()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task result = primaryUser.GameController.PostJoinSession(validGuid, PlayerColour.Black);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.ExpectationFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestGetPlayers_WithInvalidGuid()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            Task<IEnumerable<IPlayer>> result = primaryUser.GameController.GetPlayers(invalidGuid);

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
        [TestCategory("GameController")]
        public async Task TestGetPlayers_WithValidGuidAndOnePlayer()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid);

            // Act
            IEnumerable<IPlayer> result = await primaryUser.GameController.GetPlayers(validGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(primaryUser.OwnerId, result.First().UserId);
            Assert.AreEqual(primaryUser.OwnerId, result.First().Name);
        }

        #region - PostEndPhase -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestPostEndPhase_WithValidGuid()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements);
            ISession sessionDetails = await primaryUser.SessionRepository.GetSession(validGuid);

            // Act
            await primaryUser.GameController.PostEndPhase(validGuid, sessionDetails.PhaseId);

            // Assert
            Assert.AreEqual(sessionDetails.PhaseId, primaryUser.SessionRepository.SessionMap[validGuid].Players.First().CompletedPhase);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestPostEndPhase_WithInvalidGuid()
        {
            // Arrange
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.GameController.PostEndPhase(invalidGuid, invalidGuid);

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
        [TestCategory("GameController")]
        public async Task TestPostEndPhase_WithValidGuid_WithInvalidPhase()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task result = primaryUser.GameController.PostEndPhase(validGuid, Guid.Empty);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.ExpectationFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestPostEndPhase_WithValidGuid_WithInvalidUser()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupSessionPhase(SessionPhase.Reinforcements);
            ISession sessionDetails = await primaryUser.SessionRepository.GetSession(validGuid);

            // Act
            Task result = primaryUser.GameController.PostEndPhase(validGuid, sessionDetails.PhaseId);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, exception.Response.StatusCode);
            }
        }
        #endregion

        #region - AdvanceNextPhase -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithInvalidGuid()
        {
            // Arrange
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.GameController.PostAdvanceNextPhase(invalidGuid, invalidGuid, true);

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
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithInvalidPhaseId()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Guid invalidGuid = new Guid("3286C8E6-B510-4F7F-AAE0-9EF827459E7E");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid);

            // Act
            Task result = primaryUser.GameController.PostAdvanceNextPhase(validGuid, invalidGuid, true);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.ExpectationFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithNotOwner()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.GameController.PostAdvanceNextPhase(validGuid, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId, true);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithPlayersNotReady()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue);

            // Act
            Task result = primaryUser.GameController.PostAdvanceNextPhase(validGuid, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId, false);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.ExpectationFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithTwoPlayers()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree();
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.Reinforcements, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.Fail("Need to validate the number of reinforcements now available to deploy");
            Dictionary<String, int> regionsPerPlayer = new Dictionary<String, int>();
            foreach(var regionEntry in primaryUser.RegionRepository.RegionData)
            {
                Assert.AreEqual(1U, regionEntry.Value.TroopCount);
                if (regionsPerPlayer.ContainsKey(regionEntry.Value.OwnerId))
                {
                    regionsPerPlayer[regionEntry.Value.OwnerId] += 1;
                }
                else
                {
                    regionsPerPlayer[regionEntry.Value.OwnerId] = 1;
                }
            }
            Assert.AreEqual(2, regionsPerPlayer.Count);
            var regionsPerPlayerQuery = from entry in regionsPerPlayer
                                        group entry by entry.Value into regionGroups
                                        orderby regionGroups.Key ascending
                                        select regionGroups;
            Assert.AreEqual(2, regionsPerPlayerQuery.First().Key);
            Assert.AreEqual(3, regionsPerPlayerQuery.Last().Key);
        }
        #endregion
    }
}
