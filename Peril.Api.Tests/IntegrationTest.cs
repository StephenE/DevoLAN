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
        [DeploymentItem(@"Data\ValidWorldDefinition.xml", "WorldData")]
        [Ignore]
        public async Task IntegrationTestStartAndPlayOneRound_WithTwoUsers()
        {
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.RegionRepository.WorldDefinitionPath = @"WorldData\ValidWorldDefinition.xml";

            ControllerMock secondaryUser = new ControllerMock(DummyUserRepository.RegisteredUserIds[1], primaryUser);

            // Create session using primary user
            ISession sessionDetails = await primaryUser.GameController.PostStartNewSession(PlayerColour.Black);
            Assert.IsNotNull(sessionDetails);

            // Join session using secondary user
            await secondaryUser.GameController.PostJoinSession(sessionDetails.GameId, PlayerColour.Green);

            // Assert all players in session
            var playersInSession = await primaryUser.GameController.GetPlayers(sessionDetails.GameId);
            Assert.AreEqual(2, playersInSession.Count());

            // Start game (with primary user)
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Deploy initial troops
            await RandomlyDeployReinforcements(primaryUser, sessionDetails.GameId);
            await RandomlyDeployReinforcements(secondaryUser, sessionDetails.GameId);

            // Move into combat phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Issue random attack orders
            await RandomlyAttack(primaryUser, sessionDetails.GameId);
            await RandomlyAttack(secondaryUser, sessionDetails.GameId);

            // Move into resolution phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Resolve combat
            await ResolveAllCombat(primaryUser, sessionDetails.GameId);

            // Issue random deployment order
            await RandomlyRedeployTroops(primaryUser, sessionDetails.GameId);
            await RandomlyRedeployTroops(secondaryUser, sessionDetails.GameId);

            // Move into victory phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task IntegrationTestCombatResolution_WithDirectInvasion()
        {
            // Arrange
            Guid validGuid = new Guid("68E4A0DC-BAB8-4C79-A6E9-D0A7494F3B45");
            ControllerMock primaryUser = new ControllerMock();
            primaryUser.SetupDummySession(validGuid)
                       .SetupAddPlayer(DummyUserRepository.RegisteredUserIds[1], PlayerColour.Blue)
                       .SetupDummyWorldAsTree(primaryUser.OwnerId)
                       .SetupRegionOwnership(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, DummyUserRepository.RegisteredUserIds[1])
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 5)
                       .SetupRegionTroops(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 2)
                       .SetupSessionPhase(SessionPhase.CombatOrders)
                       .QueueAttack(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 4)
                       // Rig dice so that A beats B
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 6, 6, 6)
                       .SetupRiggedDiceResults(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 1, 1);

            // Act
            await ResolveAllCombat(primaryUser, validGuid);

            // Assert
            Assert.AreEqual(SessionPhase.Redeployment, primaryUser.SessionRepository.SessionMap[validGuid].PhaseType);
            var invasion = primaryUser.GetInvasion(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB);
            var invasionResults = primaryUser.WorldRepository.CombatResults[invasion.CombatId];
            AssertCombat.IsResultValid(1, invasion, invasionResults);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionA, 1, 0, invasionResults);
            AssertCombat.IsArmyResult(ControllerMockRegionRepositoryExtensions.DummyWorldRegionB, 1, 2, invasionResults);

            Assert.AreEqual(primaryUser.OwnerId, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionA].OwnerId);
            Assert.AreEqual(1U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionA].TroopCount);
            Assert.AreEqual(0U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionA].TroopsCommittedToPhase);

            Assert.AreEqual(primaryUser.OwnerId, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionB].OwnerId);
            Assert.AreEqual(4U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionB].TroopCount);
            Assert.AreEqual(0U, primaryUser.RegionRepository.RegionData[ControllerMockRegionRepositoryExtensions.DummyWorldRegionB].TroopsCommittedToPhase);
        }

        private async Task<IEnumerable<Guid>> GetCurrentlyOwnedRegions(ControllerMock user, Guid sessionId)
        {
            IEnumerable<IRegion> worldRegions = await user.WorldController.GetRegionList(sessionId);

            return from region in worldRegions
                   where region.OwnerId == user.OwnerId
                   select region.RegionId;
        }

        private async Task RandomlyDeployReinforcements(ControllerMock user, Guid sessionId)
        {
            Random rand = new Random();
            ISession session = await user.GameController.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(user, sessionId);

            // Get number of available troops
            UInt32 numberOfAvailableTroops = await user.NationController.GetReinforcements(sessionId);
            Assert.AreNotEqual(0U, numberOfAvailableTroops);

            // Distribute troops over available regions
            while(numberOfAvailableTroops > 0)
            {
                int index = rand.Next(0, ownedRegions.Count());
                Guid targetRegion = ownedRegions.ElementAt(index);
                await user.RegionController.PostDeployTroops(sessionId, targetRegion, 1);
                numberOfAvailableTroops -= 1;
            }

            // End deployment
            await user.GameController.PostEndPhase(session.GameId, session.PhaseId);
        }

        private async Task RandomlyAttack(ControllerMock user, Guid sessionId)
        {
            ISession session = await user.GameController.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(user, sessionId);
            bool hasAttacked = false;

            foreach(Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await user.RegionController.GetDetails(sessionId, ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        IRegion targetDetails = await user.RegionController.GetDetails(sessionId, adjacentRegionId);
                        if(targetDetails.OwnerId != user.OwnerId)
                        {
                            await user.RegionController.PostAttack(sessionId, ownedRegionId, details.TroopCount - 1, adjacentRegionId);
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

            // End attack phase
            await user.GameController.PostEndPhase(session.GameId, session.PhaseId);
        }

        private async Task ResolveAllCombat(ControllerMock user, Guid sessionId)
        {
            bool isInCombatRound = false;

            do
            {
                ISession session = await user.GameController.GetSession(sessionId);

                switch (session.PhaseType)
                {
                    case SessionPhase.CombatOrders:
                    case SessionPhase.BorderClashes:
                    case SessionPhase.MassInvasions:
                    case SessionPhase.Invasions:
                    case SessionPhase.SpoilsOfWar:
                        {
                            isInCombatRound = true;
                            break;
                        }
                    default:
                        {
                            isInCombatRound = false;
                            break;
                        }
                }

                // Advance to the next phase
                if (isInCombatRound)
                {
                    await user.GameController.PostAdvanceNextPhase(session.GameId, session.PhaseId, true);
                }
            }
            while (isInCombatRound);
        }

        private async Task RandomlyRedeployTroops(ControllerMock user, Guid sessionId)
        {
            ISession session = await user.GameController.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(user, sessionId);
            bool hasRedeployed = false;

            foreach (Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await user.RegionController.GetDetails(sessionId, ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        if (ownedRegions.Contains(adjacentRegionId))
                        {
                            await user.RegionController.PostRedeployTroops(sessionId, ownedRegionId, details.TroopCount - 1, adjacentRegionId);
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
            await user.GameController.PostEndPhase(session.GameId, session.PhaseId);
        }
    }
}
