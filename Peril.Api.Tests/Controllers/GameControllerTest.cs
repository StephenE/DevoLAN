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
            foreach(var regionDataEntry in primaryUser.RegionRepository.RegionData)
            {
                foreach(Guid connectedRegion in regionDataEntry.Value.ConnectedRegions)
                {
                    Assert.IsTrue(primaryUser.RegionRepository.RegionData.ContainsKey(connectedRegion), "Expected to find connected region");
                    Assert.IsTrue(primaryUser.RegionRepository.RegionData[connectedRegion].ConnectedRegions.Contains(regionDataEntry.Key), "Expected connected region to include us as a connection");
                }
            }
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
        [Ignore]
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
        public async Task TestAdvanceNextPhase_WithNotStarted_WithTwoPlayers()
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
            Assert.AreEqual(20U, primaryUser.SessionRepository.SessionMap[validGuid].Players.First().AvailableReinforcements);
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

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithReinforcing_WithTwoPlayers()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, DummyUserRepository.RegisteredUserIds[1])
                       .SetupAvailableReinforcements(primaryUser.OwnerId, 10)
                       .SetupAvailableReinforcements(DummyUserRepository.RegisteredUserIds[1], 20)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .QueueDeployReinforcements(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 15)
                       .QueueDeployReinforcements(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5)
                       .QueueDeployReinforcements(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC, 9)
                       .QueueDeployReinforcements(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.CombatOrders, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(0U, primaryUser.SessionRepository.SessionMap[validGuid].Players[0].AvailableReinforcements);
            Assert.AreEqual(0U, primaryUser.SessionRepository.SessionMap[validGuid].Players[1].AvailableReinforcements);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionA].OwnerId);
            Assert.AreEqual(20U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionA].TroopCount);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionB].OwnerId);
            Assert.AreEqual(5U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionB].TroopCount);
            Assert.AreEqual(primaryUser.OwnerId, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionC].OwnerId);
            Assert.AreEqual(9U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionC].TroopCount);
            Assert.AreEqual(primaryUser.OwnerId, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionD].OwnerId);
            Assert.AreEqual(1U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionD].TroopCount);
            Assert.AreEqual(primaryUser.OwnerId, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionE].OwnerId);
            Assert.AreEqual(0U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionE].TroopCount);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithCombatOrders_WithNoAttacks()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupSessionPhase(SessionPhase.CombatOrders);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.Redeployment, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithCombatOrders_WithBorderClash()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.BorderClashes, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(1, primaryUser.WorldRepository.BorderClashes.Count());
            Assert.AreEqual(2, primaryUser.WorldRepository.Invasions.Count());

            var borderClash = primaryUser.WorldRepository.BorderClashes.First().Value;
            Assert.AreEqual(CombatType.BorderClash, borderClash.ResolutionType);
            Assert.AreEqual(2, borderClash.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, primaryUser.OwnerId, borderClash);
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, DummyUserRepository.RegisteredUserIds[1], borderClash);

            var invasionOfA = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA);
            Assert.AreEqual(CombatType.Invasion, invasionOfA.ResolutionType);
            Assert.AreEqual(1, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfA);

            var invasionOfD = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);
            Assert.AreEqual(CombatType.Invasion, invasionOfD.ResolutionType);
            Assert.AreEqual(1, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, primaryUser.OwnerId, invasionOfD);

            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithCombatOrders_WithMassInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 2)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.MassInvasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(1, primaryUser.WorldRepository.MassInvasions.Count());
            ICombat invasion = primaryUser.WorldRepository.MassInvasions.First().Value;
            Assert.AreEqual(CombatType.MassInvasion, invasion.ResolutionType);
            Assert.AreEqual(3, invasion.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 1, primaryUser.OwnerId, invasion);
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, primaryUser.OwnerId, invasion);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, DummyUserRepository.RegisteredUserIds[1], invasion);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithCombatOrders_WithInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.Invasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(1, primaryUser.WorldRepository.Invasions.Count());
            var invasion = primaryUser.WorldRepository.Invasions.First().Value;
            Assert.AreEqual(CombatType.Invasion, invasion.ResolutionType);
            Assert.AreEqual(2, invasion.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, DummyUserRepository.RegisteredUserIds[1], invasion);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, primaryUser.OwnerId, invasion);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithCombatOrders_WithChainedInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 10)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC, 3)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 4)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, ControllerMockRegionRepositoryExtensions.DummyWorldRegionC, 7);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.Invasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(2, primaryUser.WorldRepository.Invasions.Count());

            var invasionFromAToB = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB);
            Assert.AreEqual(CombatType.Invasion, invasionFromAToB.ResolutionType);
            Assert.AreEqual(2, invasionFromAToB.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, primaryUser.OwnerId, invasionFromAToB);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 3, DummyUserRepository.RegisteredUserIds[1], invasionFromAToB);

            var invasionFromBToC = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC);
            Assert.AreEqual(CombatType.Invasion, invasionFromBToC.ResolutionType);
            Assert.AreEqual(2, invasionFromBToC.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 7, DummyUserRepository.RegisteredUserIds[1], invasionFromBToC);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC, 3, primaryUser.OwnerId, invasionFromBToC);

            Assert.AreEqual(0, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyOrderAttackQueue.Count);
            Assert.AreEqual(0, primaryUser.CommandQueue.DummyRedeployQueue.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithBorderClashes_WithResultInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Guid combatId;
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 7)
                       .SetupSessionPhase(SessionPhase.BorderClashes)
                       .SetupBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, out combatId)
                       // Rig dice rolls so that D beats A, taking 1 loss
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, 4, 6, 1, 1)
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 5, 5, 5, 2, 6);
            
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.MassInvasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(2, primaryUser.WorldRepository.Invasions.Count());
            AssertCombat.IsResultValid(2, primaryUser.WorldRepository.BorderClashes[combatId], primaryUser.WorldRepository.CombatResults[combatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 2, 4, primaryUser.WorldRepository.CombatResults[combatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, 1, primaryUser.WorldRepository.CombatResults[combatId]);

            var invasionOfA = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA);
            Assert.AreEqual(CombatType.Invasion, invasionOfA.ResolutionType);
            Assert.AreEqual(2, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, primaryUser.OwnerId, invasionOfA);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfA);

            var invasionOfD = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);
            Assert.AreEqual(CombatType.Invasion, invasionOfD.ResolutionType);
            Assert.AreEqual(1, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4, primaryUser.OwnerId, invasionOfD);

        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithBorderClashes_WithResultNoInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Guid combatId;
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 7)
                       .SetupSessionPhase(SessionPhase.BorderClashes)
                       .SetupBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, out combatId)
                       // Rig dice rolls so that D & A draw, each losing all troops
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, 2, 3, 6)
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, 2, 3, 6);
            
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.MassInvasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(2, primaryUser.WorldRepository.Invasions.Count());
            AssertCombat.IsResultValid(2, primaryUser.WorldRepository.BorderClashes[combatId], primaryUser.WorldRepository.CombatResults[combatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 2, 4, primaryUser.WorldRepository.CombatResults[combatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, 3, primaryUser.WorldRepository.CombatResults[combatId]);

            var invasionOfA = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA);
            Assert.AreEqual(CombatType.Invasion, invasionOfA.ResolutionType);
            Assert.AreEqual(1, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfA);

            var invasionOfD = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);
            Assert.AreEqual(CombatType.Invasion, invasionOfD.ResolutionType);
            Assert.AreEqual(1, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4, primaryUser.OwnerId, invasionOfD);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithBorderClashes_WithResultMassInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Guid massInvasionCombatId;
            Guid borderClashCombatId;
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 7)
                       .SetupSessionPhase(SessionPhase.BorderClashes)
                       .SetupMassInvasionWithBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 6, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, out borderClashCombatId, out massInvasionCombatId)
                       // Rig dice rolls so that D beats A, taking 1 loss
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, 1, 1, 6, 5)
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, 2, 2, 5, 1, 1, 6, 1, 1);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.MassInvasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(1, primaryUser.WorldRepository.MassInvasions.Count());
            Assert.AreEqual(1, primaryUser.WorldRepository.Invasions.Count());
            AssertCombat.IsResultValid(3, primaryUser.WorldRepository.BorderClashes[borderClashCombatId], primaryUser.WorldRepository.CombatResults[borderClashCombatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 3, 4, primaryUser.WorldRepository.CombatResults[borderClashCombatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, 1, primaryUser.WorldRepository.CombatResults[borderClashCombatId]);

            var invasionOfA = primaryUser.GetMassInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA);
            Assert.AreEqual(CombatType.MassInvasion, invasionOfA.ResolutionType);
            Assert.AreEqual(3, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2, primaryUser.OwnerId, invasionOfA);
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 6, primaryUser.OwnerId, invasionOfA);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfA);

            var invasionOfD = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);
            Assert.AreEqual(CombatType.Invasion, invasionOfD.ResolutionType);
            Assert.AreEqual(1, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4, primaryUser.OwnerId, invasionOfD);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("GameController")]
        public async Task TestAdvanceNextPhase_WithBorderClashes_WithResultTwoInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            Guid massInvasionCombatId;
            Guid borderClashCombatId;
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 7)
                       .SetupSessionPhase(SessionPhase.BorderClashes)
                       .SetupMassInvasionWithBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 4, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 6, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, out borderClashCombatId, out massInvasionCombatId)
                       // Rig dice rolls so that A beats D, taking 1 loss
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 6, 6, 3)
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 3, 3, 3);
            Guid currentSessionPhaseId = primaryUser.SessionRepository.SessionMap[validGuid].PhaseId;

            // Act
            await primaryUser.GameController.PostAdvanceNextPhase(validGuid, currentSessionPhaseId, true);

            // Assert
            Assert.AreEqual(SessionPhase.MassInvasions, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            Assert.AreNotEqual(currentSessionPhaseId, primaryUser.SessionRepository.SessionMap[validGuid].PhaseId);
            Assert.AreEqual(1, primaryUser.WorldRepository.MassInvasions.Count());
            Assert.AreEqual(1, primaryUser.WorldRepository.Invasions.Count());
            AssertCombat.IsResultValid(1, primaryUser.WorldRepository.BorderClashes[borderClashCombatId], primaryUser.WorldRepository.CombatResults[borderClashCombatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, 1, primaryUser.WorldRepository.CombatResults[borderClashCombatId]);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, 3, primaryUser.WorldRepository.CombatResults[borderClashCombatId]);

            var invasionOfA = primaryUser.GetMassInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA);
            Assert.AreEqual(CombatType.MassInvasion, invasionOfA.ResolutionType);
            Assert.AreEqual(2, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 6, primaryUser.OwnerId, invasionOfA);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfA);

            var invasionOfD = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);
            Assert.AreEqual(CombatType.Invasion, invasionOfD.ResolutionType);
            Assert.AreEqual(2, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 3, DummyUserRepository.RegisteredUserIds[1], invasionOfD);
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 4, primaryUser.OwnerId, invasionOfD);
        }
        #endregion
    }
}
