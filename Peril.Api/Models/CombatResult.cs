using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peril.Api.Models
{
    public class CombatResult : ICombatResult
    {
        public CombatResult(ICombatResult result)
        {
            CombatId = result.CombatId;
            m_Rounds = new List<CombatRoundResult>();
            foreach (ICombatRoundResult roundResult in result.Rounds)
            {
                m_Rounds.Add(new CombatRoundResult(roundResult));
            }
        }

        public CombatResult(Guid combatId)
        {
            CombatId = combatId;
            m_Rounds = new List<CombatRoundResult>();
        }

        public Guid CombatId { get; set; }

        public IEnumerable<ICombatRoundResult> Rounds { get { return m_Rounds; } }

        static public CombatResult GenerateForCombat(ICombat combat, Func<Guid, IEnumerable<UInt32>> randomNumberGenerator)
        {
            CombatResult result = new CombatResult(combat.CombatId);
            List<CombatArmy> armies = (from army in combat.InvolvedArmies
                                       select new CombatArmy(army)).ToList();

            while(armies.GroupBy(army => army.OwnerUserId).Count() > 1)
            {
                CombatRoundResult combatRound = CombatRoundResult.GenerateForCombat(combat.ResolutionType, armies, randomNumberGenerator);
                result.m_Rounds.Add(combatRound);

                // Remove armies with no troops left
                armies.RemoveAll(army => army.NumberOfTroops == 0);

                // Mass invasion has an early out when the defenders are all dead. Otherwise, fight to the last army standing!
                if(combat.ResolutionType == CombatType.MassInvasion)
                {
                    if(armies.Where(army => army.ArmyMode == CombatArmyMode.Defending).Count() == 0)
                    {
                        armies.Clear();
                    }
                }
            }

            return result;
        }

        private List<CombatRoundResult> m_Rounds;
    }
}
