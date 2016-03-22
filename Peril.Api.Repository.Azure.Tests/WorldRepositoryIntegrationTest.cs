using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;

namespace Peril.Api.Repository.Azure.Tests
{
    [TestClass]
    public class WorldRepositoryIntegrationTest
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
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddArmyToCombat()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid sessionId = new Guid("2CDE3217-B8F2-4FDA-8E7A-3B6B6FA4C747");
            Guid combatId = new Guid("0DAAF6DD-E1D6-42BA-B3ED-749BB7652C8E");
            Guid attackingRegionId = new Guid("5EA3D204-63EA-4683-913E-C5C3609BD893");
            Guid defendingRegionId = new Guid("6DC3039A-CC79-4CAC-B7CE-37E1B1565A6C");
            CombatTableEntry tableEntry = new CombatTableEntry(sessionId, combatId, CombatType.Invasion);
            tableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, sessionId, 1);
            testTable.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.InsertOrReplace(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Act
            await repository.AddArmyToCombat(sessionId, CombatType.BorderClash, new Dictionary<Guid, IEnumerable<ICombatArmy>>
            {
                {
                    defendingRegionId,  new List<ICombatArmy>
                    {
                        new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 6),
                        new CombatArmy(defendingRegionId, "AttackingUser2", Core.CombatArmyMode.Attacking, 3)
                    }
                }
            });

            // Assert
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(sessionId.ToString(), combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(sessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(CombatType.MassInvasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(2, resultStronglyTyped.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            //             ICombatArmy attackingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).FirstOrDefault();
            //             Assert.IsNotNull(attackingArmy);
            //             Assert.AreEqual(attackingRegionId, attackingArmy.OriginRegionId);
            //             Assert.AreEqual("AttackingUser", attackingArmy.OwnerUserId);
            //             Assert.AreEqual(5U, attackingArmy.NumberOfTroops);

            ICombatArmy defendingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();
            Assert.IsNotNull(defendingArmy);
            Assert.AreEqual(defendingRegionId, defendingArmy.OriginRegionId);
            Assert.AreEqual("DefendingUser", defendingArmy.OwnerUserId);
            Assert.AreEqual(4U, defendingArmy.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombat()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid sessionId = new Guid("2CDE3217-B8F2-4FDA-8E7A-3B6B6FA4C747");
            Guid attackingRegionId = new Guid("4CD8D6E1-8FFE-48E1-8FE0-B89BCDD0AA96");
            Guid defendingRegionId = new Guid("E0FE9A73-4125-4DA1-A113-25ED927EA7B4");

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, sessionId, 1);
            testTable.CreateIfNotExists();

            // Act
            var combatIds = await repository.AddCombat(sessionId, new List<Tuple<CombatType, IEnumerable<ICombatArmy>>>
            {
                Tuple.Create<CombatType, IEnumerable<ICombatArmy>>(CombatType.MassInvasion, new List<ICombatArmy>
                {
                    new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                    new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
                })
            });

            // Assert
            Guid combatId = combatIds.First();
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(sessionId.ToString(), combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(sessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(CombatType.MassInvasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(2, resultStronglyTyped.InvolvedArmies.Count());

            ICombatArmy attackingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).FirstOrDefault();
            Assert.IsNotNull(attackingArmy);
            Assert.AreEqual(attackingRegionId, attackingArmy.OriginRegionId);
            Assert.AreEqual("AttackingUser", attackingArmy.OwnerUserId);
            Assert.AreEqual(5U, attackingArmy.NumberOfTroops);

            ICombatArmy defendingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();
            Assert.IsNotNull(defendingArmy);
            Assert.AreEqual(defendingRegionId, defendingArmy.OriginRegionId);
            Assert.AreEqual("DefendingUser", defendingArmy.OwnerUserId);
            Assert.AreEqual(4U, defendingArmy.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombatResults()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombat()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombatByType()
        {
            // Arrange


            // Act


            // Assert
            Assert.Fail("Test not implemented");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public void IntegrationTestGetRandomNumberGenerator()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);

            // Act
            var randomNumbers = repository.GetRandomNumberGenerator(Guid.Empty, 1, 6).Take(100000);

            // Assert 
            Assert.AreEqual(0, randomNumbers.Count(number => number < 1));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 1));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 2));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 3));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 4));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 5));
            Assert.IsTrue(5000 > randomNumbers.Count(number => number == 6));
            Assert.AreEqual(0, randomNumbers.Count(number => number > 6));
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestCombatTableEntrySerialise()
        {
            // Arrange
            Guid sessionId = new Guid("2CDE3217-B8F2-4FDA-8E7A-3B6B6FA4C747");
            Guid combatId = new Guid("5C161C2F-3982-45B8-8FCE-9D7F609AB012");
            Guid attackingRegionId = new Guid("4CD8D6E1-8FFE-48E1-8FE0-B89BCDD0AA96");
            Guid defendingRegionId = new Guid("E0FE9A73-4125-4DA1-A113-25ED927EA7B4");
            CombatTableEntry tableEntry = new CombatTableEntry(sessionId, combatId, CombatType.Invasion);
            List<ICombatArmy> armies = new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            };
            tableEntry.SetCombatArmy(armies);
            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, sessionId, 1);
            testTable.CreateIfNotExists();

            // Act
            TableOperation insertOperation = TableOperation.InsertOrReplace(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Assert
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(sessionId.ToString(), combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(sessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(CombatType.Invasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(2, resultStronglyTyped.InvolvedArmies.Count());

            ICombatArmy attackingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking).FirstOrDefault();
            Assert.IsNotNull(attackingArmy);
            Assert.AreEqual(attackingRegionId, attackingArmy.OriginRegionId);
            Assert.AreEqual("AttackingUser", attackingArmy.OwnerUserId);
            Assert.AreEqual(5U, attackingArmy.NumberOfTroops);

            ICombatArmy defendingArmy = resultStronglyTyped.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();
            Assert.IsNotNull(defendingArmy);
            Assert.AreEqual(defendingRegionId, defendingArmy.OriginRegionId);
            Assert.AreEqual("DefendingUser", defendingArmy.OwnerUserId);
            Assert.AreEqual(4U, defendingArmy.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public void IntegrationTestCombatArmyCreateFromAzureString()
        {
            // Arrange
            String testString = "EF29EC0A-50E2-4207-9F94-D5D70C280D87#TestUser#0#5";

            // Act
            CombatArmy army = CombatArmy.CreateFromAzureString(testString);

            // Assert
            Assert.AreEqual(new Guid("EF29EC0A-50E2-4207-9F94-D5D70C280D87"), army.OriginRegionId);
            Assert.AreEqual("TestUser", army.OwnerUserId);
            Assert.AreEqual(Core.CombatArmyMode.Attacking, army.ArmyMode);
            Assert.AreEqual(5U, army.NumberOfTroops);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public void IntegrationTestEncodeToAzureString()
        {
            // Arrange
            Guid regionId = new Guid("EF29EC0A-50E2-4207-9F94-D5D70C280D87");
            CombatArmy army = new CombatArmy(regionId, "TestUser", Core.CombatArmyMode.Attacking, 5);

            // Act
            String result = army.EncodeToAzureString();

            // Assert
            Assert.AreEqual("ef29ec0a-50e2-4207-9f94-d5d70c280d87#TestUser#0#5", result);
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }

        static private CloudStorageAccount StorageAccount { get; set; }
        static private CloudTableClient TableClient { get; set; }
    }

    static class AssertCombat
    {
        static public void IsAttacking(Guid regionId, UInt32 numberOfTroops, String ownerId, ICombat combat)
        {
            var atatckingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking && army.OriginRegionId == regionId);

            Assert.AreEqual(1, atatckingArmy.Count(), "Could not find attacking army");
            Assert.AreEqual(numberOfTroops, atatckingArmy.First().NumberOfTroops);
            Assert.AreEqual(ownerId, atatckingArmy.First().OwnerUserId);
        }

        static public void IsDefending(Guid regionId, UInt32 numberOfTroops, String ownerId, ICombat combat)
        {
            var defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();

            Assert.IsNotNull(defendingArmy, "No defending army in combat");
            Assert.AreEqual(numberOfTroops, defendingArmy.NumberOfTroops);
            Assert.AreEqual(regionId, defendingArmy.OriginRegionId);
            Assert.AreEqual(ownerId, defendingArmy.OwnerUserId);
        }

        static public void IsResultValid(UInt32 numberOfRounds, ICombat sourceCombat, ICombatResult result)
        {
            Assert.AreEqual(numberOfRounds, (UInt32)result.Rounds.Count());
            Assert.AreEqual(sourceCombat.InvolvedArmies.Where(army => army.NumberOfTroops > 0).Count(), result.Rounds.First().ArmyResults.Count());

            foreach (ICombatRoundResult round in result.Rounds)
            {
                foreach (ICombatArmy army in sourceCombat.InvolvedArmies)
                {
                    var armyResults = round.ArmyResults.Where(armyResult => armyResult.OriginRegionId == army.OriginRegionId).FirstOrDefault();
                    if (armyResults != null)
                    {
                        Assert.IsNotNull(armyResults.RolledResults);
                        Assert.AreEqual(army.OwnerUserId, armyResults.OwnerUserId);
                        if (army.ArmyMode == CombatArmyMode.Defending)
                        {
                            Assert.IsTrue(2 >= armyResults.RolledResults.Count() && 1 <= armyResults.RolledResults.Count(), "Defender can only roll 1 or 2 dice");
                        }
                        else
                        {
                            Assert.IsTrue(3 >= armyResults.RolledResults.Count() && 1 <= armyResults.RolledResults.Count(), "Attacker can only roll 1, 2 or 3 dice");
                        }

                        switch (sourceCombat.ResolutionType)
                        {
                            case CombatType.BorderClash:
                                Assert.IsTrue(3 >= armyResults.TroopsLost, "An army cannot lose more than three troops in a round");
                                break;
                            case CombatType.Invasion:
                                Assert.IsTrue(2 >= armyResults.TroopsLost, "An army cannot lose more than two troops in a round");
                                break;
                        }
                    }
                }
            }
        }

        static public void IsArmyResult(Guid regionId, UInt32 numberOfSurvivedRounds, UInt32 expectedTroopLoss, ICombatResult result)
        {
            UInt32 roundsSurvived = 0;
            UInt32 troopsLost = 0;
            foreach (ICombatRoundResult round in result.Rounds)
            {
                var armyResults = round.ArmyResults.Where(armyResult => armyResult.OriginRegionId == regionId).FirstOrDefault();
                if (roundsSurvived == numberOfSurvivedRounds)
                {
                    Assert.IsNull(armyResults);
                }
                else
                {
                    Assert.IsNotNull(armyResults);
                    troopsLost += armyResults.TroopsLost;
                    roundsSurvived += 1;
                }
            }

            Assert.AreEqual(expectedTroopLoss, troopsLost);
        }
    }
}
