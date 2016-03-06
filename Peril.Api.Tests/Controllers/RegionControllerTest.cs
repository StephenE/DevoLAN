using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Controllers.Api;
using Peril.Api.Models;
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
            Task<IRegion> result = primaryUser.RegionController.GetDetails(InvalidRegionGuid);

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

            // Act
            IRegion result = await primaryUser.RegionController.GetDetails(OwnedRegionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(OwnedRegionGuid, result.RegionId);
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

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(InvalidRegionGuid, 1);

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

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(UnownedRegionGuid, 1);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(OwnedRegionGuid, 1);

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

            // Act
            await primaryUser.RegionController.PostDeployTroops(OwnedRegionGuid, 1);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostDeployTroops(OwnedRegionGuid, 10);

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

            // Act
            Task result = primaryUser.RegionController.PostAttack(InvalidRegionGuid, 1, InvalidRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostAttack(OwnedRegionGuid, 1, InvalidRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostAttack(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostAttack(UnownedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostAttack(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostAttack(OwnedRegionGuid, 1, UnownedRegionGuid);

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

            // Act
            await primaryUser.RegionController.PostAttack(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostAttack(OwnedRegionGuid, 10, UnownedAdjacentRegionGuid);

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

        #region - Get Attack -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestGetAttack_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<AttackDetails>> result = primaryUser.RegionController.GetAttack(InvalidRegionGuid);

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
        public async Task TestGetAttack_WithUnownedRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<AttackDetails>> result = primaryUser.RegionController.GetAttack(UnownedRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestGetAttack_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task<IEnumerable<AttackDetails>> result = primaryUser.RegionController.GetAttack(OwnedRegionGuid);

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
        public async Task TestGetAttack_WithValidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            IEnumerable<AttackDetails> result = await primaryUser.RegionController.GetAttack(OwnedRegionGuid);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(OwnedRegionGuid, result.First().SourceRegion);
            Assert.AreEqual(UnownedAdjacentRegionGuid, result.First().TargetRegion);
            Assert.AreEqual(1, result.First().NumberOfTroops);
        }
        #endregion

        #region - Delete Attack -
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestDeleteAttack_WithInvalidRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.DeleteAttack(InvalidRegionGuid, InvalidRegionGuid);

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
        public async Task TestDeleteAttack_WithValidRegion_WithInvalidTargetRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.DeleteAttack(OwnedRegionGuid, InvalidRegionGuid);

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
        public async Task TestDeletetAttack_WithUnownedRegion()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.DeleteAttack(UnownedRegionGuid, UnownedAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestDeleteAttack_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

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
        public async Task TestDeleteAttack_WithValidRegion_WithValidAttack()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            await primaryUser.RegionController.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestDeleteAttack_WithValidRegion_WithInvalidAttack()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            await primaryUser.RegionController.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

            // Assert
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

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(InvalidRegionGuid, 1, InvalidRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 1, InvalidRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(UnownedRegionGuid, 1, OwnedAdjacentRegionGuid);

            // Assert
            try
            {
                await result;
                Assert.Fail("Expected exception to be thrown");
            }
            catch (HttpResponseException exception)
            {
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidRound()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 1, OwnedNonAdjacentRegionGuid);

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

            // Act
            await primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            ControllerMock primaryUser = new ControllerMock();

            // Act
            Task result = primaryUser.RegionController.PostRedeployTroops(OwnedRegionGuid, 10, OwnedAdjacentRegionGuid);

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

        Guid InvalidRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
        Guid OwnedRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid OwnedAdjacentRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid OwnedNonAdjacentRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid UnownedRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
        Guid UnownedAdjacentRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
    }
}
