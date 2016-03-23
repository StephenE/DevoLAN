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
    public class WorldControllerTest
    {
        #region - GetRegionList -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetRegionList_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<IRegion>> result = primaryUser.WorldController.GetRegionList(SessionGuid);

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
        [TestCategory("WorldController")]
        public async Task TestGetRegionList_WithNotInSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task<IEnumerable<IRegion>> result = primaryUser.WorldController.GetRegionList(SessionGuid);

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
        [TestCategory("WorldController")]
        public async Task TestGetRegionList_WithNoRegions()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid);

            // Act
            IEnumerable<IRegion> result = await primaryUser.WorldController.GetRegionList(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetRegionList_WithValidRegions()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree();

            // Act
            IEnumerable<IRegion> result = await primaryUser.WorldController.GetRegionList(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Count());
        }
        #endregion

        #region - GetCombat -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<ICombat>> result = primaryUser.WorldController.GetCombat(SessionGuid);

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
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithNotInSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1]);

            // Act
            Task<IEnumerable<ICombat>> result = primaryUser.WorldController.GetCombat(SessionGuid);

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
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithNoCombat()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithBorderClash()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 7)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 2)
                       .SetupBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());

            var borderClash = result.Where(combat => combat.ResolutionType == CombatType.BorderClash).FirstOrDefault();
            Assert.IsNotNull(borderClash);
            Assert.AreEqual(2, borderClash.InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, primaryUser.OwnerId, borderClash);
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, DummyUserRepository.RegisteredUserIds[1], borderClash);

            var invasionOfA = result.Where(combat => combat.ResolutionType == CombatType.Invasion && combat.InvolvedArmies.First().OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionA).FirstOrDefault();
            Assert.IsNotNull(invasionOfA);
            Assert.AreEqual(1, invasionOfA.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 2, primaryUser.OwnerId, invasionOfA);

            var invasionOfD = result.Where(combat => combat.ResolutionType == CombatType.Invasion && combat.InvolvedArmies.First().OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).FirstOrDefault();
            Assert.IsNotNull(invasionOfD);
            Assert.AreEqual(1, invasionOfD.InvolvedArmies.Count());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, DummyUserRepository.RegisteredUserIds[1], invasionOfD);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithMassInvasion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, DummyUserRepository.RegisteredUserIds[2])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 10)
                       .SetupMassInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(CombatType.MassInvasion, result.First().ResolutionType);
            Assert.AreEqual(3, result.First().InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5, DummyUserRepository.RegisteredUserIds[1], result.First());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, DummyUserRepository.RegisteredUserIds[2], result.First());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 10, primaryUser.OwnerId, result.First());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithInvasion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1)
                       .SetupInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(CombatType.Invasion, result.First().ResolutionType);
            Assert.AreEqual(2, result.First().InvolvedArmies.Count());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, primaryUser.OwnerId, result.First());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, DummyUserRepository.RegisteredUserIds[1], result.First());
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetCombat_WithSpoilsOfWar()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, DummyUserRepository.RegisteredUserIds[2])
                       .SetupSpoilsOfWar(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(CombatType.SpoilsOfWar, result.First().ResolutionType);
            Assert.AreEqual(3, result.First().InvolvedArmies.Count());

            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5, DummyUserRepository.RegisteredUserIds[1], result.First());
            AssertCombat.IsAttacking(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1, DummyUserRepository.RegisteredUserIds[2], result.First());
            AssertCombat.IsDefending(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 0, primaryUser.OwnerId, result.First());
        }
        #endregion

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
    }
}
