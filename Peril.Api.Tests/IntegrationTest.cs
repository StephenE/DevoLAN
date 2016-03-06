using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Tests.Controllers;
using Peril.Api.Tests.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task IntegrationTestStartGame_WithTwoUsers()
        {
            ControllerMock primaryUser = new ControllerMock();
            ControllerMock secondaryUser = new ControllerMock(DummyUserRepository.RegisteredUserIds[1], primaryUser);

            // Create session using primary user
            ISession sessionDetails = await primaryUser.GameController.PostStartNewSession();
            Assert.IsNotNull(sessionDetails);

            // Join session using secondary user
            await secondaryUser.GameController.PostJoinSession(sessionDetails.GameId);

            // Assert all players in session
            var playersInSession = await primaryUser.GameController.GetPlayers(sessionDetails.GameId);
            Assert.AreEqual(2, playersInSession.Count());

            // Start game (with primary user)

            // Deploy initial troops
            await RandomlyDeployReinforcements(primaryUser);
            await RandomlyDeployReinforcements(secondaryUser);

            // Move into combat round (with primary user)

            // Issue random attack orders
            await RandomlyAttack(primaryUser);
            await RandomlyAttack(secondaryUser);

            // Move into resolution round (with primary user)

            // Resolve combat

            // Move into redeployment round (with primary user)

            // Issue random deployment order
            await RandomlyRedeployTroops(primaryUser);
            await RandomlyRedeployTroops(secondaryUser);

            // Move into victory round (with primary user)
        }

        private async Task RandomlyDeployReinforcements(ControllerMock user)
        {
            Random rand = new Random();

            // Get owned regions
            IEnumerable<Guid> ownedRegions = null;

            // Get number of available troops
            int numberOfAvailableTroops = 0;

            // Distribute troops over available regions
            while(numberOfAvailableTroops > 0)
            {
                int index = rand.Next(0, ownedRegions.Count());
                Guid targetRegion = ownedRegions.ElementAt(index);
                await user.RegionController.PostDeployTroops(targetRegion, 1);
                numberOfAvailableTroops -= 1;
            }

            // End deployment
        }

        private async Task RandomlyAttack(ControllerMock user)
        {
            // Get owned regions
            IEnumerable<Guid> ownedRegions = null;
            bool hasAttacked = false;

            foreach(Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await user.RegionController.GetDetails(ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        IRegion targetDetails = await user.RegionController.GetDetails(adjacentRegionId);
                        if(targetDetails.OwnerId != user.OwnerId)
                        {
                            await user.RegionController.PostAttack(ownedRegionId, details.TroopCount - 1, adjacentRegionId);
                            hasAttacked = true;
                            break;
                        }
                    }
                }

                if(hasAttacked)
                {
                    break;
                }
            }

            // End attack round
        }

        private async Task RandomlyRedeployTroops(ControllerMock user)
        {
            // Get owned regions
            IEnumerable<Guid> ownedRegions = null;
            bool hasRedeployed = false;

            foreach (Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await user.RegionController.GetDetails(ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        if (ownedRegions.Contains(adjacentRegionId))
                        {
                            await user.RegionController.PostRedeployTroops(ownedRegionId, details.TroopCount - 1, adjacentRegionId);
                            hasRedeployed = true;
                            break;
                        }
                    }
                }

                if (hasRedeployed)
                {
                    break;
                }
            }

            // End redeployment
        }
    }
}
