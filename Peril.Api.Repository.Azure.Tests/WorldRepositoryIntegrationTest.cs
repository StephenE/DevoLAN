using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.DeleteIfExists();
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddArmyToCombat()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid combatId = new Guid("0DAAF6DD-E1D6-42BA-B3ED-749BB7652C8E");
            Guid attackingRegionId = new Guid("5EA3D204-63EA-4683-913E-C5C3609BD893");
            Guid attacking2RegionId = new Guid("E0675161-4192-4C33-B8BB-3B6D763725E2");
            Guid attacking3RegionId = new Guid("CA563328-5743-4EC0-AA39-D7978DE44872");
            Guid defendingRegionId = new Guid("6DC3039A-CC79-4CAC-B7CE-37E1B1565A6C");
            CombatTableEntry tableEntry = new CombatTableEntry(SessionId, 1, combatId, CombatType.MassInvasion);
            tableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Act
            await repository.AddArmyToCombat(SessionId, 1, CombatType.BorderClash, new Dictionary<Guid, IEnumerable<ICombatArmy>>
            {
                {
                    defendingRegionId,  new List<ICombatArmy>
                    {
                        new CombatArmy(attacking2RegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 6),
                        new CombatArmy(attacking3RegionId, "AttackingUser2", Core.CombatArmyMode.Attacking, 3)
                    }
                }
            });

            // Assert
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(SessionId.ToString(), "Combat_" + combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(SessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(1, resultStronglyTyped.Round);
            Assert.AreEqual(CombatType.MassInvasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(4, resultStronglyTyped.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsAttacking(attacking2RegionId, 6, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsAttacking(attacking3RegionId, 3, "AttackingUser2", resultStronglyTyped);
            AssertCombat.IsDefending(defendingRegionId, 4, "DefendingUser", resultStronglyTyped);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombat()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid attackingRegionId = new Guid("4CD8D6E1-8FFE-48E1-8FE0-B89BCDD0AA96");
            Guid defendingRegionId = new Guid("E0FE9A73-4125-4DA1-A113-25ED927EA7B4");

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();

            // Act
            var combatIds = await repository.AddCombat(SessionId, 1, new List<Tuple<CombatType, IEnumerable<ICombatArmy>>>
            {
                Tuple.Create<CombatType, IEnumerable<ICombatArmy>>(CombatType.MassInvasion, new List<ICombatArmy>
                {
                    new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                    new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
                })
            });

            // Assert
            Guid combatId = combatIds.First();
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(SessionId.ToString(), "Combat_" + combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(SessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(CombatType.MassInvasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(2, resultStronglyTyped.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsDefending(defendingRegionId, 4, "DefendingUser", resultStronglyTyped);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestAddCombatResults()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid SessionId = new Guid("2CDE3217-B8F2-4FDA-8E7A-3B6B6FA4C747");
            Guid combatId = new Guid("4B0286E6-6DBE-4F86-A87C-1CF776F41437");
            Guid attackingRegionId = new Guid("4CD8D6E1-8FFE-48E1-8FE0-B89BCDD0AA96");
            Guid defendingRegionId = new Guid("E0FE9A73-4125-4DA1-A113-25ED927EA7B4");
            CombatTableEntry combat = new CombatTableEntry(SessionId, 1, combatId, CombatType.Invasion);
            combat.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 3),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 2)
            });
            CombatResultTableEntry tableEntry = new CombatResultTableEntry(SessionId, combatId, new List<ICombatRoundResult>
            {
                new CombatRoundResult(
                    new List <ICombatArmyRoundResult>
                    {
                        new CombatArmyRoundResult(attackingRegionId, "AttackingUser", new List<UInt32> { 2, 3, 4 }, 1),
                        new CombatArmyRoundResult(defendingRegionId, "DefendingUser", new List<UInt32> { 1, 1 }, 2),
                    }
                )
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();

            // Act
            await repository.AddCombatResults(SessionId, 1, new List<ICombatResult>
            {
                tableEntry
            });

            // Assert
            TableOperation operation = TableOperation.Retrieve<CombatResultTableEntry>(SessionId.ToString(), "Result_" + combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatResultTableEntry));
            CombatResultTableEntry resultStronglyTyped = result.Result as CombatResultTableEntry;
            AssertCombat.IsResultValid(1, combat, resultStronglyTyped);

            AssertCombat.IsArmyResult(attackingRegionId, 1, 1, resultStronglyTyped);
            AssertCombat.IsArmyResult(defendingRegionId, 1, 2, resultStronglyTyped);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombat()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid combatId = new Guid("44C850BA-711E-4E5E-9537-E612DA18E15E");
            Guid attackingRegionId = new Guid("5EA3D204-63EA-4683-913E-C5C3609BD893");
            Guid defendingRegionId = new Guid("6DC3039A-CC79-4CAC-B7CE-37E1B1565A6C");
            CombatTableEntry tableEntry = new CombatTableEntry(SessionId, 1, combatId, CombatType.Invasion);
            tableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Act
            var results = await repository.GetCombat(SessionId, 1);

            // Assert
            Assert.IsNotNull(results);

            ICombat result = results.Where(combat => combat.CombatId == combatId).FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.AreEqual(combatId, result.CombatId);
            Assert.AreEqual(CombatType.Invasion, result.ResolutionType);
            Assert.AreEqual(2, result.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", result);
            AssertCombat.IsDefending(defendingRegionId, 4, "DefendingUser", result);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombatByType()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid combatId = new Guid("B75CFB8A-727A-46C1-A952-BF2B1AFF9AD8");
            Guid secondCombatId = new Guid("2F366A82-A99C-4A83-BF0E-FFF8D87D94A6");
            Guid attackingRegionId = new Guid("5EA3D204-63EA-4683-913E-C5C3609BD893");
            Guid defendingRegionId = new Guid("6DC3039A-CC79-4CAC-B7CE-37E1B1565A6C");
            CombatTableEntry tableEntry = new CombatTableEntry(SessionId, 1, combatId, CombatType.Invasion);
            tableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });
            CombatTableEntry secondTableEntry = new CombatTableEntry(SessionId, 1, secondCombatId, CombatType.SpoilsOfWar);
            secondTableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(tableEntry);
            await testTable.ExecuteAsync(insertOperation);
            insertOperation = TableOperation.Insert(secondTableEntry);
            await testTable.ExecuteAsync(insertOperation);

            CombatTableEntry thirdTableEntry = new CombatTableEntry(SessionId, 2, Guid.NewGuid(), CombatType.SpoilsOfWar);
            insertOperation = TableOperation.Insert(thirdTableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Act
            var results = await repository.GetCombat(SessionId, 1, CombatType.SpoilsOfWar);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count());

            ICombat result = results.Where(combat => combat.CombatId == combatId).FirstOrDefault();
            Assert.IsNull(result);

            result = results.Where(combat => combat.CombatId == secondCombatId).FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.AreEqual(CombatType.SpoilsOfWar, result.ResolutionType);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public async Task IntegrationTestGetCombatWithMultipleRounds()
        {
            // Arrange
            WorldRepository repository = new WorldRepository(DevelopmentStorageAccountConnectionString);
            Guid combatId = new Guid("3233BE80-37BA-4FBD-B07B-BB18F6E47FEE");
            Guid attackingRegionId = new Guid("5EA3D204-63EA-4683-913E-C5C3609BD893");
            Guid defendingRegionId = new Guid("6DC3039A-CC79-4CAC-B7CE-37E1B1565A6C");
            CombatTableEntry tableEntry = new CombatTableEntry(SessionId, 5, combatId, CombatType.Invasion);
            tableEntry.SetCombatArmy(new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            });

            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            CombatTableEntry otherRoundTableEntry = new CombatTableEntry(SessionId, 2, Guid.NewGuid(), CombatType.Invasion);
            insertOperation = TableOperation.Insert(otherRoundTableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Act
            var results = await repository.GetCombat(SessionId, 5);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count());

            ICombat result = results.Where(combat => combat.CombatId == combatId).FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.AreEqual(combatId, result.CombatId);
            Assert.AreEqual(CombatType.Invasion, result.ResolutionType);
            Assert.AreEqual(2, result.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", result);
            AssertCombat.IsDefending(defendingRegionId, 4, "DefendingUser", result);
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
            Guid combatId = new Guid("5C161C2F-3982-45B8-8FCE-9D7F609AB012");
            Guid attackingRegionId = new Guid("4CD8D6E1-8FFE-48E1-8FE0-B89BCDD0AA96");
            Guid defendingRegionId = new Guid("E0FE9A73-4125-4DA1-A113-25ED927EA7B4");
            CombatTableEntry tableEntry = new CombatTableEntry(SessionId, 1, combatId, CombatType.Invasion);
            List<ICombatArmy> armies = new List<ICombatArmy>
            {
                new CombatArmy(attackingRegionId, "AttackingUser", Core.CombatArmyMode.Attacking, 5),
                new CombatArmy(defendingRegionId, "DefendingUser", Core.CombatArmyMode.Defending, 4)
            };
            tableEntry.SetCombatArmy(armies);
            CloudTable testTable = SessionRepository.GetTableForSessionData(TableClient, SessionId);
            testTable.CreateIfNotExists();

            // Act
            TableOperation insertOperation = TableOperation.Insert(tableEntry);
            await testTable.ExecuteAsync(insertOperation);

            // Assert
            TableOperation operation = TableOperation.Retrieve<CombatTableEntry>(SessionId.ToString(), "Combat_" + combatId.ToString());
            TableResult result = await testTable.ExecuteAsync(operation);
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(CombatTableEntry));
            CombatTableEntry resultStronglyTyped = result.Result as CombatTableEntry;
            Assert.AreEqual(SessionId, resultStronglyTyped.SessionId);
            Assert.AreEqual(combatId, resultStronglyTyped.CombatId);
            Assert.AreEqual(1, resultStronglyTyped.Round);
            Assert.AreEqual(CombatType.Invasion, resultStronglyTyped.ResolutionType);
            Assert.AreEqual(2, resultStronglyTyped.InvolvedArmies.Count());

            AssertCombat.IsAttacking(attackingRegionId, 5, "AttackingUser", resultStronglyTyped);
            AssertCombat.IsDefending(defendingRegionId, 4, "DefendingUser", resultStronglyTyped);
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
        public void IntegrationTestCombatArmyEncodeToAzureString()
        {
            // Arrange
            Guid regionId = new Guid("EF29EC0A-50E2-4207-9F94-D5D70C280D87");
            CombatArmy army = new CombatArmy(regionId, "TestUser", Core.CombatArmyMode.Attacking, 5);

            // Act
            String result = army.EncodeToAzureString();

            // Assert
            Assert.AreEqual("ef29ec0a-50e2-4207-9f94-d5d70c280d87#TestUser#0#5", result);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public void IntegrationTestCombatRoundResultCreateFromAzureString()
        {
            // Arrange
            String testString = "E63BC819-A1D2-4876-AE76-BBD63EBAEC99#AttackingUser#1#2#3#4@223AA4AE-AF1A-4169-A39C-3E6BA8F5B981#DefendingUser#2#1";

            // Act
            CombatRoundResult result = CombatRoundResult.CreateFromAzureString(testString);

            // Assert
            CombatResultTableEntry resultHelper = new CombatResultTableEntry(Guid.NewGuid(), Guid.NewGuid(), new List<ICombatRoundResult> { result });
            AssertCombat.IsArmyResult(new Guid("E63BC819-A1D2-4876-AE76-BBD63EBAEC99"), 1, 1, resultHelper);
            AssertCombat.IsArmyResult(new Guid("223AA4AE-AF1A-4169-A39C-3E6BA8F5B981"), 1, 2, resultHelper);
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WorldRepository")]
        public void IntegrationTestCombatRoundResultEncodeToAzureString()
        {
            // Arrange
            CombatRoundResult round = new CombatRoundResult(new List<ICombatArmyRoundResult>
            {
                new CombatArmyRoundResult(new Guid("E63BC819-A1D2-4876-AE76-BBD63EBAEC99"), "AttackingUser", new List<UInt32> { 2, 3, 4 }, 1),
                new CombatArmyRoundResult(new Guid("223AA4AE-AF1A-4169-A39C-3E6BA8F5B981"), "DefendingUser", new List<UInt32> { 1 }, 2),
            });

            // Act
            String encoded = round.EncodeToAzureString();

            // Assert
            Assert.AreEqual("e63bc819-a1d2-4876-ae76-bbd63ebaec99#AttackingUser#1#2#3#4@223aa4ae-af1a-4169-a39c-3e6ba8f5b981#DefendingUser#2#1", encoded);
        }

        static private String DevelopmentStorageAccountConnectionString
        {
            get { return "UseDevelopmentStorage=true"; }
        }
        static private Guid SessionId
        {
            get { return new Guid("2CDE3217-B8F2-4FDA-8E7A-3B6B6FA4C747"); }
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
