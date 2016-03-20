using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    public class DummyWorldRepository : IWorldRepository
    {
        public DummyWorldRepository()
        {
            TargetRegionToCombatLookup = new Dictionary<Guid, List<DummyCombat>>();
            BorderClashes = new Dictionary<Guid, DummyCombat>();
            MassInvasions = new Dictionary<Guid, DummyCombat>();
            Invasions = new Dictionary<Guid, DummyCombat>();
            SpoilsOfWar = new Dictionary<Guid, DummyCombat>();
            CombatResults = new Dictionary<Guid, ICombatResult>();
            RegionNumberGeneratorInjections = new Dictionary<Guid, Queue<int>>();
            NumberGenerator = new Random(0);
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            List<ICombat> combat = BorderClashes.Select(combatPair => combatPair.Value as ICombat).ToList();
            combat.AddRange(MassInvasions.Select(combatPair => combatPair.Value));
            combat.AddRange(Invasions.Select(combatPair => combatPair.Value));
            combat.AddRange(SpoilsOfWar.Select(combatPair => combatPair.Value));
            return Task.FromResult<IEnumerable<ICombat>>(combat);
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType combatType)
        {
            switch(combatType)
            {
                case CombatType.BorderClash:
                    return Task.FromResult<IEnumerable<ICombat>>(BorderClashes.Values);
                case CombatType.MassInvasion:
                    return Task.FromResult<IEnumerable<ICombat>>(MassInvasions.Values);
                case CombatType.Invasion:
                    return Task.FromResult<IEnumerable<ICombat>>(Invasions.Values);
                case CombatType.SpoilsOfWar:
                    return Task.FromResult<IEnumerable<ICombat>>(SpoilsOfWar.Values);
            }

            throw new InvalidOperationException();
        }

        public Task AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            foreach(var combatData in armies)
            {
                Guid combatId = Guid.NewGuid();
                DummyCombat combat = new DummyCombat(combatId, combatData.Item1);

                foreach(ICombatArmy army in combatData.Item2)
                {
                    combat.SetupAddArmy(army.OriginRegionId, army.OwnerUserId, army.ArmyMode, army.NumberOfTroops);
                }

                // Add to lookup
                AddToCombatLookup(combat);

                // Add to typed storage
                switch (combat.ResolutionType)
                {
                    case CombatType.BorderClash:
                        BorderClashes[combatId] = combat;
                        break;
                    case CombatType.MassInvasion:
                        MassInvasions[combatId] = combat;
                        break;
                    case CombatType.Invasion:
                        Invasions[combatId] = combat;
                        break;
                    case CombatType.SpoilsOfWar:
                        SpoilsOfWar[combatId] = combat;
                        break;
                }
            }

            return Task.FromResult(false);
        }

        public Task AddArmyToCombat(Guid sessionId, CombatType sourceType, IDictionary<Guid, IEnumerable<ICombatArmy>> armies)
        {
            foreach(var combatEntry in armies)
            {
                Guid targetRegionId = combatEntry.Key;
                if (TargetRegionToCombatLookup.ContainsKey(targetRegionId))
                {
                    foreach(DummyCombat combat in TargetRegionToCombatLookup[targetRegionId])
                    {
                        if (sourceType < combat.ResolutionType)
                        {
                            foreach (ICombatArmy army in combatEntry.Value)
                            {
                                combat.SetupAddArmy(army.OriginRegionId, army.OwnerUserId, army.ArmyMode, army.NumberOfTroops);
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unable to find the target region in the combat lookup");
                }
            }

            return Task.FromResult(false);
        }

        public Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results)
        {
            foreach(ICombatResult result in results)
            {
                CombatResults[result.CombatId] = result;
            }

            return Task.FromResult(false);
        }

        public IEnumerable<Int32> GetRandomNumberGenerator(Guid regionId, int minimum, int maximum)
        {
            if (RegionNumberGeneratorInjections.ContainsKey(regionId))
            {
                while (RegionNumberGeneratorInjections[regionId].Count > 0)
                {
                    yield return RegionNumberGeneratorInjections[regionId].Dequeue();
                }
                throw new InvalidOperationException("Ran out of pre-defined numbers for region");
            }

            yield return NumberGenerator.Next(minimum, maximum);
        }

        #region - Test Helpers -
        public void AddToCombatLookup(DummyCombat combat)
        {
            switch(combat.ResolutionType)
            {
                case CombatType.BorderClash:
                    // Never need to look up border clash by target region
                    break;
                default:
                    // Find the defending side and add this combat
                    var defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).First();
                    if(!TargetRegionToCombatLookup.ContainsKey(defendingArmy.OriginRegionId))
                    {
                        TargetRegionToCombatLookup[defendingArmy.OriginRegionId] = new List<DummyCombat>();
                    }
                    TargetRegionToCombatLookup[defendingArmy.OriginRegionId].Add(combat);
                    break;
            }
        }
        #endregion

        public Dictionary<Guid, List<DummyCombat>> TargetRegionToCombatLookup { get; private set; }
        public Dictionary<Guid, DummyCombat> BorderClashes { get; private set; }
        public Dictionary<Guid, DummyCombat> MassInvasions { get; private set; }
        public Dictionary<Guid, DummyCombat> Invasions { get; private set; }
        public Dictionary<Guid, DummyCombat> SpoilsOfWar { get; private set; }
        public Dictionary<Guid, ICombatResult> CombatResults { get; private set; }
        public Random NumberGenerator { get; set; }
        public Dictionary<Guid, Queue<int>> RegionNumberGeneratorInjections { get; set; }
    }

    static class ControllerMockWorldRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupBorderClash(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId;
            return SetupBorderClash(setupContext, attackingRegion, attackingTroops, secondAttackingRegion, secondAttackingTroops, out combatId);
        }

        static public ControllerMockSetupContext SetupBorderClash(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops, out Guid combatId)
        {
            SetupBorderClashWithoutPendingInvasions(setupContext, attackingRegion, attackingTroops, secondAttackingRegion, secondAttackingTroops, out combatId);
            SetupInvasionPendingBorderClash(setupContext, attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].TroopCount - attackingTroops);
            SetupInvasionPendingBorderClash(setupContext, secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].TroopCount - secondAttackingTroops);
            return setupContext;
        }

        static private ControllerMockSetupContext SetupBorderClashWithoutPendingInvasions(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops, out Guid combatId)
        {
            combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.BorderClash);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            setupContext.ControllerMock.WorldRepository.BorderClashes[combatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasion(this ControllerMockSetupContext setupContext, Guid targetRegion, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.MassInvasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].TroopCount);
            setupContext.ControllerMock.WorldRepository.MassInvasions[combatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasionWithBorderClash(this ControllerMockSetupContext setupContext, Guid targetRegion, UInt32 counterAttackingTroops, Guid attackingRegion, UInt32 attackingTroops, Guid counterAttackedRegion, UInt32 secondAttackingTroops)
        {
            Guid borderClashCombatId;
            Guid massInvasionCombatId;
            return SetupMassInvasionWithBorderClash(setupContext, targetRegion, counterAttackingTroops, attackingRegion, attackingTroops, counterAttackedRegion, secondAttackingTroops, out borderClashCombatId, out massInvasionCombatId);
        }

        static public ControllerMockSetupContext SetupMassInvasionWithBorderClash(this ControllerMockSetupContext setupContext, Guid targetRegion, UInt32 counterAttackingTroops, Guid attackingRegion, UInt32 attackingTroops, Guid counterAttackedRegion, UInt32 secondAttackingTroops, out Guid borderClashCombatId, out Guid massInvasionCombatId)
        {
            massInvasionCombatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(massInvasionCombatId, CombatType.MassInvasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].TroopCount - counterAttackingTroops);
            setupContext.ControllerMock.WorldRepository.MassInvasions[massInvasionCombatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);

            SetupBorderClashWithoutPendingInvasions(setupContext, targetRegion, counterAttackingTroops, counterAttackedRegion, secondAttackingTroops, out borderClashCombatId);
            SetupInvasionPendingBorderClash(setupContext, counterAttackedRegion, setupContext.ControllerMock.RegionRepository.RegionData[counterAttackedRegion].TroopCount - secondAttackingTroops);
            return setupContext;
        }

        static private ControllerMockSetupContext SetupInvasionPendingBorderClash(this ControllerMockSetupContext setupContext, Guid defendingRegion, UInt32 defendingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.Invasion);
            combat.SetupAddArmy(defendingRegion, setupContext.ControllerMock.RegionRepository.RegionData[defendingRegion].OwnerId, CombatArmyMode.Defending, defendingTroops);
            setupContext.ControllerMock.WorldRepository.Invasions[combatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupInvasion(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid defendingRegion, UInt32 defendingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.Invasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(defendingRegion, setupContext.ControllerMock.RegionRepository.RegionData[defendingRegion].OwnerId, CombatArmyMode.Defending, defendingTroops);
            setupContext.ControllerMock.WorldRepository.Invasions[combatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupSpoilsOfWar(this ControllerMockSetupContext setupContext, Guid targetRegion, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.SpoilsOfWar);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, 0);
            setupContext.ControllerMock.WorldRepository.SpoilsOfWar[combatId] = combat;
            setupContext.ControllerMock.WorldRepository.AddToCombatLookup(combat);
            return setupContext;
        }

        static public ControllerMockSetupContext SetupRiggedDiceResults(this ControllerMockSetupContext setupContext, Guid targetRegion, params int[] rolls)
        {
            if(!setupContext.ControllerMock.WorldRepository.RegionNumberGeneratorInjections.ContainsKey(targetRegion))
            {
                setupContext.ControllerMock.WorldRepository.RegionNumberGeneratorInjections[targetRegion] = new Queue<int>();
            }
            foreach(int roll in rolls)
            {
                setupContext.ControllerMock.WorldRepository.RegionNumberGeneratorInjections[targetRegion].Enqueue(roll);
            }
            return setupContext;
        }

        static public ICombat GetMassInvasion(this ControllerMock controller, Guid defendingRegion)
        {
            var query = from combatEntry in controller.WorldRepository.MassInvasions
                        let combat = combatEntry.Value
                        let defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault()
                        where defendingArmy.OriginRegionId == defendingRegion
                        select combat;

            return query.FirstOrDefault();
        }

        static public ICombat GetInvasion(this ControllerMock controller, Guid defendingRegion)
        {
            var query = from combatEntry in controller.WorldRepository.Invasions
                        let combat = combatEntry.Value
                        let defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault()
                        where defendingArmy.OriginRegionId == defendingRegion
                        select combat;

            return query.FirstOrDefault();
        }
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
            Assert.AreEqual(sourceCombat.InvolvedArmies.Count(), result.Rounds.First().ArmyResults.Count());

            foreach (ICombatRoundResult round in result.Rounds)
            {
                foreach (ICombatArmy army in sourceCombat.InvolvedArmies)
                {
                    var armyResults = round.ArmyResults.Where(armyResult => armyResult.OriginRegionId == army.OriginRegionId).FirstOrDefault();
                    if (armyResults != null)
                    {
                        Assert.IsNotNull(armyResults.RolledResults);
                        Assert.AreEqual(army.OwnerUserId, armyResults.OwnerUserId);
                        if(army.ArmyMode == CombatArmyMode.Defending)
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
                if(roundsSurvived == numberOfSurvivedRounds)
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
