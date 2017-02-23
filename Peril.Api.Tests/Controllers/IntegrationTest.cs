using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Controllers.Api;
using Peril.Api.Repository;
using Peril.Api.Repository.Azure;
using Peril.Api.Repository.Azure.Model;
using Peril.Api.Tests.Repository;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Controllers
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        [DeploymentItem(@"Data\ValidWorldDefinition.xml", "WorldData")]
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

        internal static async Task<IEnumerable<Guid>> GetCurrentlyOwnedRegions(WorldController world, Guid sessionId, String ownerId)
        {
            IEnumerable<IRegion> worldRegions = await world.GetRegionList(sessionId);

            return from region in worldRegions
                   where region.OwnerId == ownerId
                   select region.RegionId;
        }

        private async Task RandomlyDeployReinforcements(ControllerMock user, Guid sessionId)
        {
            await RandomlyDeployReinforcements(user.GameController, user.NationController, user.WorldController, user.RegionController, sessionId, user.OwnerId);
        }

        internal static async Task RandomlyDeployReinforcements(GameController game, NationController nations, WorldController world, RegionController regions, Guid sessionId, String ownerId)
        {
            Random rand = new Random();
            ISession session = await game.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(world, sessionId, ownerId);

            // Get number of available troops
            UInt32 numberOfAvailableTroops = await nations.GetReinforcements(sessionId);
            Assert.AreNotEqual(0U, numberOfAvailableTroops);

            // Distribute troops over available regions
            Dictionary<Guid, UInt32> regionsToReinforce = new Dictionary<Guid, UInt32>();
            while (numberOfAvailableTroops > 0 && ownedRegions.Count() > 0)
            {
                int index = rand.Next(0, ownedRegions.Count());
                Guid targetRegion = ownedRegions.ElementAt(index);
                if (regionsToReinforce.ContainsKey(targetRegion))
                {
                    regionsToReinforce[targetRegion] += 1;
                }
                else
                {
                    regionsToReinforce.Add(targetRegion, 1);
                }
                numberOfAvailableTroops -= 1;
            }

            // Perform the deployment
            foreach(var pair in regionsToReinforce)
            {
                await regions.PostDeployTroops(sessionId, pair.Key, pair.Value);
            }

            // End deployment
            await game.PostEndPhase(session.GameId, session.PhaseId);
        }

        private async Task RandomlyAttack(ControllerMock user, Guid sessionId)
        {
            await RandomlyAttack(user.GameController, user.WorldController, user.RegionController, sessionId, user.OwnerId, UInt32.MaxValue);
        }

        internal static async Task<bool> RandomlyAttack(GameController game, WorldController world, RegionController regions, Guid sessionId, String ownerId, UInt32 troopCount)
        {
            ISession session = await game.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(world, sessionId, ownerId);
            bool hasAttacked = false;

            foreach (Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await regions.GetDetails(sessionId, ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        IRegion targetDetails = await regions.GetDetails(sessionId, adjacentRegionId);
                        if (targetDetails.OwnerId != ownerId)
                        {
                            UInt32 troopsToAttackWith = Math.Min(details.TroopCount - 1, troopCount);
                            await regions.PostAttack(sessionId, ownedRegionId, troopsToAttackWith, adjacentRegionId);
                            hasAttacked = true;
                            break;
                        }
                    }
                }

                if (hasAttacked)
                {
                    break;
                }
            }

            // End attack phase
            await game.PostEndPhase(session.GameId, session.PhaseId);

            return hasAttacked;
        }

        internal static async Task<bool> BulkRandomlyAttack(GameController game, WorldController world, RegionRepository regionRepository, SessionRepository sessionRepository, Guid sessionId, String ownerId, UInt32 troopCount, UInt32 numberOfAttacks)
        {
            ISession session = await game.GetSession(sessionId);

            // Get owned regions
            List<CommandQueueTableEntry> attacksToQueue = new List<CommandQueueTableEntry>();
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(world, sessionId, ownerId);
            IEnumerable<IRegionData> worldRegionList = await regionRepository.GetRegions(sessionId);
            Dictionary<Guid, IRegionData> worldRegionLookup = worldRegionList.ToDictionary(region => region.RegionId);
            bool hasAttacked = false;

            // Create attack table entries
            foreach (Guid ownedRegionId in ownedRegions)
            {
                IRegionData details = worldRegionLookup[ownedRegionId];
                if (details.TroopCount > 1 && numberOfAttacks > 0)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        IRegionData targetDetails = worldRegionLookup[adjacentRegionId];
                        if (targetDetails.OwnerId != ownerId)
                        {
                            UInt32 troopsInRegionToAttackWith = details.TroopCount - 1;
                            UInt32 troopsToAttackWith = Math.Min(details.TroopCount - 1, troopCount);
                            while(troopsInRegionToAttackWith > 0 && numberOfAttacks > 0)
                            {
                                attacksToQueue.Add(CommandQueueTableEntry.CreateAttackMessage(session.GameId, session.PhaseId, details.RegionId, details.CurrentEtag, targetDetails.RegionId, troopsToAttackWith));
                                troopsInRegionToAttackWith -= troopsToAttackWith;
                                numberOfAttacks -= 1;
                                hasAttacked = true;
                            }
                        }

                        if (numberOfAttacks == 0)
                        {
                            break;
                        }
                    }
                }

                if(numberOfAttacks == 0)
                {
                    break;
                }
            }

            // Batch insert operations
            using (BatchOperationHandle batchOperation = new BatchOperationHandle(sessionRepository.GetTableForSessionData(sessionId)))
            {
                for (int counter = 0; counter < attacksToQueue.Count; ++counter)
                {
                    batchOperation.BatchOperation.Insert(attacksToQueue[counter]);

                    if(batchOperation.RemainingCapacity == 0)
                    {
                        await batchOperation.CommitBatch();
                    }
                }
            }

            // End attack phase
            await game.PostEndPhase(session.GameId, session.PhaseId);

            return hasAttacked;
        }

        private async Task ResolveAllCombat(ControllerMock user, Guid sessionId)
        {
            await ResolveAllCombat(user.GameController, sessionId);
        }


        internal static async Task ResolveAllCombat(GameController game, Guid sessionId)
        {
            bool isInCombatRound = false;

            do
            {
                ISession session = await game.GetSession(sessionId);

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
                    await game.PostAdvanceNextPhase(session.GameId, session.PhaseId, true);
                }
            }
            while (isInCombatRound);
        }

        private async Task RandomlyRedeployTroops(ControllerMock user, Guid sessionId)
        {
            await RandomlyRedeployTroops(user.GameController, user.WorldController, user.RegionController, sessionId, user.OwnerId);
        }

        internal static async Task RandomlyRedeployTroops(GameController game, WorldController world, RegionController regions, Guid sessionId, String ownerId)
        {
            ISession session = await game.GetSession(sessionId);

            // Get owned regions
            IEnumerable<Guid> ownedRegions = await GetCurrentlyOwnedRegions(world, sessionId, ownerId);
            bool hasRedeployed = false;

            foreach (Guid ownedRegionId in ownedRegions)
            {
                IRegion details = await regions.GetDetails(sessionId, ownedRegionId);
                if (details.TroopCount > 1)
                {
                    foreach (Guid adjacentRegionId in details.ConnectedRegions)
                    {
                        if (ownedRegions.Contains(adjacentRegionId))
                        {
                            await regions.PostRedeployTroops(sessionId, ownedRegionId, details.TroopCount - 1, adjacentRegionId);
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
            await game.PostEndPhase(session.GameId, session.PhaseId);
        }
    }
}
