using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Tests.Repository;
using Peril.Core;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Tests.Controllers
{
    [TestClass]
    public class RegionControllerTest
    {
        #region - Get Details -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestGetDetails_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IRegion> result = primaryUser.RegionController.GetDetails(SessionGuid, InvalidRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestGetDetails_WithValidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree();

            // Act
            IRegion result = await primaryUser.RegionController.GetDetails(SessionGuid, OwnedRegionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(OwnedRegionGuid, result.RegionId);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestGetDetails_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(InvalidRegionGuid)
                       .SetupDummyWorldAsTree();
            primaryUser.SessionRepository.SessionMap.Clear();

            // Act
            Task<IRegion> result = primaryUser.RegionController.GetDetails(SessionGuid, OwnedRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestGetDetails_WithUnownedSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupDummyWorldAsTree(DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task<IRegion> result = primaryUser.RegionController.GetDetails(SessionGuid, OwnedRegionGuid);

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

        #region - Post Deploy Troops -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(SessionGuid, InvalidRegionGuid, 1);

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
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithUnownedRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(SessionGuid, UnownedRegionGuid, 1);

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
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(SessionGuid, OwnedRegionGuid, 1);

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
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithValidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupAvailableReinforcements(1)
                       .SetupDummyWorldAsTree();

            // Act
            Guid operationId = await primaryUser.RegionController.PostDeployTroops(SessionGuid, OwnedRegionGuid, 1);

            // Assert
            Assert.AreEqual(1, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.Count());
            Assert.AreEqual(operationId, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.First().OperationId);
            Assert.AreEqual(OwnedRegionGuid, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.First().TargetRegion);
            Assert.AreEqual(1U, primaryUser.CommandQueue.DummyDeployReinforcementsQueue.First().NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupAvailableReinforcements(9)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(SessionGuid, OwnedRegionGuid, 10);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            }
        }
        #endregion

        #region - Post Attack -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, InvalidRegionGuid, 1, InvalidRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidTargetRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, InvalidRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithOwnedTargetRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithUnownedRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, UnownedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithUnconnectedTarget()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PaymentRequired, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithValidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(OwnedRegionGuid, 2);

            // Act
            Guid operationId = await primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
            Assert.AreEqual(1, primaryUser.CommandQueue.DummyOrderAttackQueue.Count());
            Assert.AreEqual(operationId, primaryUser.CommandQueue.DummyOrderAttackQueue.First().OperationId);
            Assert.AreEqual(UnownedAdjacentRegionGuid, primaryUser.CommandQueue.DummyOrderAttackQueue.First().TargetRegion);
            Assert.AreEqual(1U, primaryUser.CommandQueue.DummyOrderAttackQueue.First().NumberOfTroops);
            // Assert.AreEqual(1U, primaryUser.RegionRepository.RegionData[OwnedRegionGuid].TroopsCommittedToPhase);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithDuplicatedRegion_WithValidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(OwnedRegionGuid, 3);

            // Act
            Guid operationId = await primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);
            Guid secondOperationId = await primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
            Assert.AreEqual(2, primaryUser.CommandQueue.DummyOrderAttackQueue.Count());
            Assert.AreEqual(operationId, primaryUser.CommandQueue.DummyOrderAttackQueue.First().OperationId);
            Assert.AreEqual(UnownedAdjacentRegionGuid, primaryUser.CommandQueue.DummyOrderAttackQueue.First().TargetRegion);
            Assert.AreEqual(1U, primaryUser.CommandQueue.DummyOrderAttackQueue.First().NumberOfTroops);
            // Assert.AreEqual(2U, primaryUser.RegionRepository.RegionData[OwnedRegionGuid].TroopsCommittedToPhase);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(OwnedRegionGuid, 10);

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 10, UnownedAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        [Ignore]
        public async Task TestPostAttack_WithDuplicateRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(OwnedRegionGuid, 10);

            // Act
            Task result = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 9, UnownedAdjacentRegionGuid);
            Task result2 = primaryUser.RegionController.PostAttack(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
            await result;
            try
            {
                await result2;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            }
        }
        #endregion

        #region - Post Redeploy -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, InvalidRegionGuid, 1, InvalidRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidTargetRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 1, InvalidRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithUnownedTargetRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree()
                       .SetupRegionTroops(OwnedRegionGuid, 2)
                       .SetupRegionOwnership(UnownedAdjacentRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithUnownedRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(UnownedRegionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, UnownedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithUnconnectedTarget()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 1, OwnedNonAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PaymentRequired, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithValidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree()
                       .SetupRegionTroops(OwnedRegionGuid, 2);

            // Act
            Guid operationId = await primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

            // Assert
            Assert.AreEqual(1, primaryUser.CommandQueue.DummyRedeployQueue.Count());
            Assert.AreEqual(operationId, primaryUser.CommandQueue.DummyRedeployQueue.First().OperationId);
            Assert.AreEqual(OwnedAdjacentRegionGuid, primaryUser.CommandQueue.DummyRedeployQueue.First().TargetRegion);
            Assert.AreEqual(1U, primaryUser.CommandQueue.DummyRedeployQueue.First().NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Redeployment)
                       .SetupDummyWorldAsTree()
                       .SetupRegionTroops(OwnedRegionGuid, 10);

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(SessionGuid, OwnedRegionGuid, 10, OwnedAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            }
        }
        #endregion

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
        Guid InvalidRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
        Guid OwnedRegionGuid { get { return ControllerMockRegionRepositoryExtensions.DummyWorldRegionA; } }
        Guid OwnedAdjacentRegionGuid { get { return ControllerMockRegionRepositoryExtensions.DummyWorldRegionB; } }
        Guid OwnedNonAdjacentRegionGuid { get { return ControllerMockRegionRepositoryExtensions.DummyWorldRegionC; } }
        Guid UnownedRegionGuid { get { return ControllerMockRegionRepositoryExtensions.DummyWorldRegionE; } }
        Guid UnownedAdjacentRegionGuid { get { return ControllerMockRegionRepositoryExtensions.DummyWorldRegionD; } }
    }
}
