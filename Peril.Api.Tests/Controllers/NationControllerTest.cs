using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Tests.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peril.Api.Tests.Controllers
{
    [TestClass]
    public class NationControllerTest
    {
        #region - Get Reinforcements -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("NationController")]
        public async Task TestGetReinforcements_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<UInt32> result = primaryUser.NationController.GetReinforcements(InvalidSessionGuid);

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
        [TestCategory("NationController")]
        public async Task TestGetReinforcements_WithIncorrectSessionPhase()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.CombatOrders);

            // Act
            Task<UInt32> result = primaryUser.NationController.GetReinforcements(SessionGuid);

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
        [TestCategory("NationController")]
        public async Task TestGetReinforcements_WithUnjoinedSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task<UInt32> result = primaryUser.NationController.GetReinforcements(SessionGuid);

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
        [TestCategory("NationController")]
        public async Task TestGetReinforcements_WithValidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupAvailableReinforcements(10);

            // Act
            UInt32 result = await primaryUser.NationController.GetReinforcements(SessionGuid);

            // Assert
            Assert.AreEqual(10U, result);
        }
        #endregion

        #region - Get Cards -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("NationController")]
        public async Task TestGetCards_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<ICard>> result = primaryUser.NationController.GetCards(InvalidSessionGuid);

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
        [TestCategory("NationController")]
        public async Task TestGetCards_WithUnjoinedSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task<IEnumerable<ICard>> result = primaryUser.NationController.GetCards(SessionGuid);

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
        [TestCategory("NationController")]
        public async Task TestGetCards_WithValidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Yellow)
                       .SetupDummyWorldAsTree()
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC)
                       .SetupCardOwner(DummyUserRepository.RegisteredUserIds[1], ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);

            // Act
            IEnumerable<ICard> result = await primaryUser.NationController.GetCards(SessionGuid);

            // Assert
            Assert.IsNotNull(result);
            List<ICard> cards = result.ToList();
            Assert.AreEqual(3, cards.Count());
            Assert.AreEqual(1, cards.Count(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionA == card.RegionId));
            Assert.AreEqual(3U, cards.First(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionA == card.RegionId).Value);
            Assert.AreEqual(1, cards.Count(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionB == card.RegionId));
            Assert.AreEqual(5U, cards.First(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionB == card.RegionId).Value);
            Assert.AreEqual(1, cards.Count(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionC == card.RegionId));
            Assert.AreEqual(7U, cards.First(card => ControllerMockRegionRepositoryExtensions.DummyWorldRegionC == card.RegionId).Value);
        }
        #endregion

        #region - Post Cards -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("NationController")]
        public async Task TestPostCards_WithInvalidSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.NationController.PostCards(InvalidSessionGuid, new List<Guid> { });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithUnjoinedSession()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid, DummyUserRepository.RegisteredUserIds[1])
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> { });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithIncorrectSessionPhase()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC)
                       .SetupCardOwner(DummyUserRepository.RegisteredUserIds[1], ControllerMockRegionRepositoryExtensions.DummyWorldRegionD)
                       .SetupSessionPhase(SessionPhase.CombatOrders);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionC
            });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithNoCards()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, null);

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithTooFewCards()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB
            });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithTooManyCards()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionC,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionD
            });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithUnownedCard()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionC
            });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithInvalidCombinationOfCard()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD);

            // Act
            Task result = primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionD
            });

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
        [TestCategory("NationController")]
        public async Task TestPostCards_WithThreeOfAKind()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionD)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionE);

            // Act
            await primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionD,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionE
            });

            // Assert
            Assert.AreEqual(3, primaryUser.GetNation(SessionGuid, primaryUser.OwnerId).AvailableReinforcements);
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("NationController")]
        public async Task TestPostCards_WithSet()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(SessionGuid)
                       .SetupDummyWorldAsTree()
                       .SetupSessionPhase(SessionPhase.Reinforcements)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB)
                       .SetupCardOwner(ControllerMockRegionRepositoryExtensions.DummyWorldRegionC);

            // Act
            await primaryUser.NationController.PostCards(SessionGuid, new List<Guid> {
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionA,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionB,
                ControllerMockRegionRepositoryExtensions.DummyWorldRegionC
            });

            // Assert
            Assert.AreEqual(9, primaryUser.GetNation(SessionGuid, primaryUser.OwnerId).AvailableReinforcements);
        }
        #endregion

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
        Guid InvalidSessionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
    }
}
