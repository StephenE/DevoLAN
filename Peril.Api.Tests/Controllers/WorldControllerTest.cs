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
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("WorldController")]
        public async Task TestGetRegionList_WithInvalidRegion()
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

        Guid SessionGuid { get { return new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45"); } }
    }
}
