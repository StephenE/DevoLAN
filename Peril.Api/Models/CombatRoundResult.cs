using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peril.Api.Models
{
    public class CombatRoundResult : ICombatRoundResult
    {
        public CombatRoundResult(ICombatRoundResult result)
        {
            m_ArmyResults = new List<CombatArmyRoundResult>();
            foreach (ICombatArmyRoundResult roundResult in result.ArmyResults)
            {
                m_ArmyResults.Add(new CombatArmyRoundResult(roundResult));
            }
        }

        private CombatRoundResult()
        {
            m_ArmyResults = new List<CombatArmyRoundResult>();
        }

        public IEnumerable<ICombatArmyRoundResult> ArmyResults { get { return m_ArmyResults; } }

        static public CombatRoundResult GenerateForCombat(CombatType resolutionMode, List<CombatArmy> armies, Func<Guid, IEnumerable<UInt32>> randomNumberGenerator)
        {
            CombatRoundResult roundResult = new CombatRoundResult();

            // Generate the results for each army
            foreach(CombatArmy army in armies)
            {
                roundResult.m_ArmyResults.Add(CombatArmyRoundResult.GenerateForCombat(army, randomNumberGenerator(army.OriginRegionId)));
            }

            switch (resolutionMode)
            {
                case CombatType.BorderClash:
                case CombatType.SpoilsOfWar:
                    {
                    for (int counter = 0; counter < 3; ++counter)
                    {
                        var attackingDiceQuery = from armyResult in roundResult.m_ArmyResults
                                                 where armyResult.RolledResults.Count() > counter
                                                 join army in armies on armyResult.OriginRegionId equals army.OriginRegionId
                                                 let armyDiceRoll = armyResult.RolledResults.ElementAt(counter)
                                                 orderby armyDiceRoll descending, army.NumberOfTroops ascending
                                                 select new { Army = army, Results = armyResult, AttackerRoll = armyDiceRoll };

                        var attackers = attackingDiceQuery.ToList();
                        for(int attackerIndex = 0; attackerIndex < attackers.Count; ++attackerIndex)
                        {
                            var attacker = attackers[attackerIndex];

                            // Compare against all remaining attackers (or until we run out of troops)
                            for (int defenderIndex = attackerIndex + 1; attacker.Results.TroopsLost < attacker.Army.NumberOfTroops && attackerIndex < attackers.Count; ++attackerIndex)
                            {
                                var defender = attackers[attackerIndex];
                                // Ensure defender has troops left and this wouldn't be friendly fire
                                if (defender.Army.OwnerUserId != attacker.Army.OwnerUserId && defender.Results.TroopsLost < defender.Army.NumberOfTroops)
                                {
                                    // We've sorted out players by dice roll, so anyone in a lower index must either draw or be worse
                                    if (attacker.AttackerRoll == defender.AttackerRoll)
                                    {
                                        attacker.Results.TroopsLost += 1;
                                    }
                                    defender.Results.TroopsLost += 1;
                                }
                            }
                        }
                    }
                    break;
                }
                case CombatType.MassInvasion:
                case CombatType.Invasion:
                {
                    // Calculate troop loses. On a tie, the defenders lose
                    var defendingDiceQuery = from armyResult in roundResult.m_ArmyResults
                                             join army in armies on armyResult.OriginRegionId equals army.OriginRegionId
                                             where army.ArmyMode == CombatArmyMode.Defending
                                             select new { Army = army, Results = armyResult, DefenderRolls = armyResult.RolledResults.OrderByDescending(diceRoll => diceRoll).ToList() };

                    var attackingDiceQuery = from armyResult in roundResult.m_ArmyResults
                                             join army in armies on armyResult.OriginRegionId equals army.OriginRegionId
                                             where army.ArmyMode == CombatArmyMode.Attacking
                                             orderby army.NumberOfTroops ascending
                                             select new { Army = army, Results = armyResult, AttackerRolls = armyResult.RolledResults.OrderByDescending(diceRoll => diceRoll).ToList() };

                    var defender = defendingDiceQuery.FirstOrDefault();
                    for (int counter = 0; counter < defender.DefenderRolls.Count; ++counter)
                    {
                        UInt32 defenderRoll = defender.DefenderRolls[counter];
                        foreach(var attacker in attackingDiceQuery)
                        {
                            // Check this attacker still has any dice left and both sides still have troops
                            if(counter < attacker.AttackerRolls.Count && attacker.Results.TroopsLost < attacker.Army.NumberOfTroops && defender.Results.TroopsLost < defender.Army.NumberOfTroops)
                            {
                                UInt32 attackerRoll = attacker.AttackerRolls[counter];
                                if(attackerRoll >= defenderRoll)
                                {
                                    defender.Results.TroopsLost += 1;
                                }
                                else
                                {
                                    attacker.Results.TroopsLost += 1;
                                }
                            }
                        }
                    }
                    break;
                }
            }

            // Apply troop loses to the armies
            var armyQuery = from armyResult in roundResult.m_ArmyResults
                            join army in armies on armyResult.OriginRegionId equals army.OriginRegionId
                            select new { Army = army, TroopsLost = armyResult.TroopsLost };
            foreach(var army in armyQuery)
            {
                army.Army.NumberOfTroops -= army.TroopsLost;
            }

            return roundResult;
        }

        private List<CombatArmyRoundResult> m_ArmyResults;
    }
}
