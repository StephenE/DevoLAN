using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Controllers.Api;
using Peril.Api.Models;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
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
            RegionController controller = CreateRegionController();

            // Act
            Task<IRegion> result = controller.GetDetails(InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            IRegion result = await controller.GetDetails(OwnedRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostDeployTroops(InvalidRegionGuid, 1);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostDeployTroops(UnownedRegionGuid, 1);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostDeployTroops(OwnedRegionGuid, 1);

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
            RegionController controller = CreateRegionController();

            // Act
            await controller.PostDeployTroops(OwnedRegionGuid, 1);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostDeployTroops_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostDeployTroops(OwnedRegionGuid, 10);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(InvalidRegionGuid, 1, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(OwnedRegionGuid, 1, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(UnownedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(OwnedRegionGuid, 1, UnownedRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            await controller.PostAttack(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostAttack_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostAttack(OwnedRegionGuid, 10, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task<IEnumerable<AttackDetails>> result = controller.GetAttack(InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task<IEnumerable<AttackDetails>> result = controller.GetAttack(UnownedRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task<IEnumerable<AttackDetails>> result = controller.GetAttack(OwnedRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            IEnumerable<AttackDetails> result = await controller.GetAttack(OwnedRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.DeleteAttack(InvalidRegionGuid, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.DeleteAttack(OwnedRegionGuid, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.DeleteAttack(UnownedRegionGuid, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            await controller.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestDeleteAttack_WithValidRegion_WithInvalidAttack()
        {
            // Arrange
            RegionController controller = CreateRegionController();

            // Act
            await controller.DeleteAttack(OwnedRegionGuid, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(InvalidRegionGuid, 1, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(OwnedRegionGuid, 1, InvalidRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(OwnedRegionGuid, 1, UnownedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(UnownedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(OwnedRegionGuid, 1, OwnedNonAdjacentRegionGuid);

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
            RegionController controller = CreateRegionController();

            // Act
            await controller.PostRedeployTroops(OwnedRegionGuid, 1, OwnedAdjacentRegionGuid);

            // Assert
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("RegionController")]
        public async Task TestPostRedeploy_WithValidRegion_WithInvalidTroops()
        {
            // Arrange
            RegionController controller = CreateRegionController();

            // Act
            Task result = controller.PostRedeployTroops(OwnedRegionGuid, 10, OwnedAdjacentRegionGuid);

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

        RegionController CreateRegionController()
        {
            RegionController controller = new RegionController();
            GenericIdentity identity = new GenericIdentity("DummyUser", "Dummy");
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "DummyUser"));
            controller.ControllerContext.RequestContext.Principal = new GenericPrincipal(identity, null);
            return controller;
        }

        Guid InvalidRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
        Guid OwnedRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid OwnedAdjacentRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid OwnedNonAdjacentRegionGuid { get { return new Guid("54901E57-862D-4223-8C57-F2FFB2EBD77C"); } }
        Guid UnownedRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
        Guid UnownedAdjacentRegionGuid { get { return new Guid("024D1E45-EF34-4AB1-840D-79229CCDE8C3"); } }
    }
}
