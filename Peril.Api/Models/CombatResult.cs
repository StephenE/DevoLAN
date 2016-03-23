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

        public IEnumerable<ICombatArmy> StartingArmies { get { return m_StartingArmies; } }

        public IEnumerable<CombatArmy> SurvivingArmies { get { return m_SurvivingArmies; } }

        static public CombatResult GenerateForCombat(ICombat combat, Func<Guid, IEnumerable<UInt32>> randomNumberGenerator)
        {
            CombatResult result = new CombatResult(combat.CombatId);
            result.m_StartingArmies = combat.InvolvedArmies;
            result.m_SurvivingArmies = (from army in combat.InvolvedArmies
                                        select new CombatArmy(army)).ToList();

            // Remove any armies with no troops (e.g. spoils of war, the defender is just here so we know what region's at stake!)
            result.m_SurvivingArmies.RemoveAll(army => army.NumberOfTroops == 0);

            while (result.m_SurvivingArmies.GroupBy(army => army.OwnerUserId).Count() > 1)
            {
                CombatRoundResult combatRound = CombatRoundResult.GenerateForCombat(combat.ResolutionType, result.m_SurvivingArmies, randomNumberGenerator);
                result.m_Rounds.Add(combatRound);

                // Remove armies with no troops left
                result.m_SurvivingArmies.RemoveAll(army => army.NumberOfTroops == 0);

                // Mass invasion has an early out when the defenders are all dead. Otherwise, fight to the last army standing!
                if (combat.ResolutionType == CombatType.MassInvasion)
                {
                    if (result.m_SurvivingArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).Count() == 0)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private List<CombatRoundResult> m_Rounds;
        private IEnumerable<ICombatArmy> m_StartingArmies;
        private List<CombatArmy> m_SurvivingArmies;
    }
}
