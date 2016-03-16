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
        public async Task TestGetDetails_WithValidSession()
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

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
        Guid InvalidSessionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
    }
}
