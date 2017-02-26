using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Peril.Api.Repository.Azure.Tests;
using Peril.Api.Tests.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests
{
    [TestClass]
    public class IntegrationTest
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            CloudStorageEmulatorShepherd shepherd = new CloudStorageEmulatorShepherd();
            shepherd.Start();

            StorageAccount = CloudStorageAccount.Parse(DevelopmentStorageAccountConnectionString);
            TableClient = StorageAccount.CreateCloudTableClient();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [DeploymentItem(@"Data\ValidWorldDefinition.xml", "WorldData")]
        public async Task IntegrationTestStartAndPlay()
        {
            var primaryUser = new ControllerAzure(DevelopmentStorageAccountConnectionString, @"WorldData\ValidWorldDefinition.xml");
            var secondaryUser = new ControllerAzure(DevelopmentStorageAccountConnectionString, @"WorldData\ValidWorldDefinition.xml", DummyUserRepository.RegisteredUserIds[1]);

            // Start new session
            var sessionDetails = await primaryUser.GameController.PostStartNewSession(Core.PlayerColour.Black);
            await secondaryUser.GameController.PostJoinSession(sessionDetails.GameId, Core.PlayerColour.Green);

            // Assert all players in session
            var playersInSession = await primaryUser.GameController.GetPlayers(sessionDetails.GameId);
            Assert.AreEqual(2, playersInSession.Count());

            // Start game (with primary user)
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Deploy initial troops for primary user
            await Controllers.IntegrationTest.RandomlyDeployReinforcements(primaryUser.GameController, primaryUser.NationController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId);

            // Move into combat phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Issue random attack orders for primary user
            bool didAttack = await Controllers.IntegrationTest.RandomlyAttack(primaryUser.GameController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId, UInt32.MaxValue);
            Assert.AreEqual(true, didAttack);

            // Move into resolution phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

            // Resolve combat
            await Controllers.IntegrationTest.ResolveAllCombat(primaryUser.GameController, sessionDetails.GameId);

            // Issue random deployment order
            await Controllers.IntegrationTest.RandomlyRedeployTroops(primaryUser.GameController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId);

            // Move into victory phase (with primary user)
            sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
            await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [DeploymentItem(@"Data\ValidWorldDefinition.xml", "WorldData")]
        public async Task IntegrationTestStartAndPlayUntilVictory()
        {
            var primaryUser = new ControllerAzure(DevelopmentStorageAccountConnectionString, @"WorldData\ValidWorldDefinition.xml");
            var secondaryUser = new ControllerAzure(DevelopmentStorageAccountConnectionString, @"WorldData\ValidWorldDefinition.xml", DummyUserRepository.RegisteredUserIds[1]);

            // Start new session
            var sessionDetails = await primaryUser.GameController.PostStartNewSession(Core.PlayerColour.Black);
            await secondaryUser.GameController.PostJoinSession(sessionDetails.GameId, Core.PlayerColour.Green);

            // Assert all players in session
            var playersInSession = await primaryUser.GameController.GetPlayers(sessionDetails.GameId);
            Assert.AreEqual(2, playersInSession.Count());

            int roundCounter = 0;
            while (true)
            {
                // Start game (with primary user)
                sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
                await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

                // Deploy initial troops for primary user
                await Controllers.IntegrationTest.RandomlyDeployReinforcements(primaryUser.GameController, primaryUser.NationController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId);

                // Move into combat phase (with primary user)
                sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
                await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

                // Issue random attack orders for primary user
                bool didAttack = await Controllers.IntegrationTest.RandomlyAttack(primaryUser.GameController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId, UInt32.MaxValue);

                // Move into resolution phase (with primary user)
                sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
                await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

                // Resolve combat
                await Controllers.IntegrationTest.ResolveAllCombat(primaryUser.GameController, sessionDetails.GameId);

                // Issue random deployment order
                await Controllers.IntegrationTest.RandomlyRedeployTroops(primaryUser.GameController, primaryUser.WorldController, primaryUser.RegionController, sessionDetails.GameId, primaryUser.OwnerId);

                // Move into victory phase (with primary user)
                sessionDetails = await primaryUser.GameController.GetSession(sessionDetails.GameId);
                await primaryUser.GameController.PostAdvanceNextPhase(sessionDetails.GameId, sessionDetails.PhaseId, true);

                // Early out when there's only the primary player left
                var regions = await primaryUser.AzureRegionRepository.GetRegions(sessionDetails.GameId);
                if (regions.Where(region => region.OwnerId != primaryUser.OwnerId).Count() == 0)
                {
                    break;
                }
                else
                {
                    ++roundCounter;
                    Assert.AreNotEqual(10, roundCounter);
                }
            }
        }

        private static String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        private static CloudStorageAccount StorageAccount { get; set; }
        private static object TableClient { get; set; }
    }
}
