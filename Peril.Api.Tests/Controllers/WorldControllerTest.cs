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
                       .SetupBorderClash(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(primaryUser.WorldRepository.BorderClashes[0].CombatId, result.First().CombatId);
            Assert.AreEqual(CombatType.BorderClash, result.First().ResolutionType);
            Assert.AreEqual(2, result.First().InvolvedArmies.Count());
            Assert.AreEqual(5U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionA).First().NumberOfTroops);
            Assert.AreEqual(primaryUser.OwnerId, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionA).First().OwnerUserId);
            Assert.AreEqual(1U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().NumberOfTroops);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().OwnerUserId);
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
                       .SetupMassInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 10U, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(primaryUser.WorldRepository.MassInvasions[0].CombatId, result.First().CombatId);
            Assert.AreEqual(CombatType.MassInvasion, result.First().ResolutionType);
            Assert.AreEqual(3, result.First().InvolvedArmies.Count());
            Assert.AreEqual(10U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().NumberOfTroops);
            Assert.AreEqual(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OriginRegionId);
            Assert.AreEqual(primaryUser.OwnerId, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OwnerUserId);
            Assert.AreEqual(5U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionB).First().NumberOfTroops);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionB).First().OwnerUserId);
            Assert.AreEqual(1U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().NumberOfTroops);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[2], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().OwnerUserId);
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
                       .SetupInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5, ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, 1);

            // Act
            IEnumerable<ICombat> result = await primaryUser.WorldController.GetCombat(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(primaryUser.WorldRepository.Invasions[0].CombatId, result.First().CombatId);
            Assert.AreEqual(CombatType.Invasion, result.First().ResolutionType);
            Assert.AreEqual(2, result.First().InvolvedArmies.Count());
            Assert.AreEqual(5U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).First().NumberOfTroops);
            Assert.AreEqual(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).First().OriginRegionId);
            Assert.AreEqual(primaryUser.OwnerId, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).First().OwnerUserId);
            Assert.AreEqual(1U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().NumberOfTroops);
            Assert.AreEqual(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OriginRegionId);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OwnerUserId);
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
            Assert.AreEqual(primaryUser.WorldRepository.SpoilsOfWar[0].CombatId, result.First().CombatId);
            Assert.AreEqual(CombatType.SpoilsOfWar, result.First().ResolutionType);
            Assert.AreEqual(3, result.First().InvolvedArmies.Count());
            Assert.AreEqual(0U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().NumberOfTroops);
            Assert.AreEqual(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OriginRegionId);
            Assert.AreEqual(primaryUser.OwnerId, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First().OwnerUserId);
            Assert.AreEqual(5U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionB).First().NumberOfTroops);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[1], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionB).First().OwnerUserId);
            Assert.AreEqual(1U, result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().NumberOfTroops);
            Assert.AreEqual(DummyUserRepository.RegisteredUserIds[2], result.First().InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).Where(army => army.OriginRegionId == ControllerMockRegionRepositoryExtensions.DummyWorldRegionD).First().OwnerUserId);
        }
        #endregion

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
    }
}
