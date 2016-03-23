using Peril.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peril.Api.Repository.Azure.Model
{
    public class CombatRoundResult : ICombatRoundResult
    {
        public CombatRoundResult(IEnumerable<ICombatArmyRoundResult> results)
        {
            ArmyResults = results;
        }

        private CombatRoundResult()
        {

        }

        public IEnumerable<ICombatArmyRoundResult> ArmyResults { get; set; }

        static public CombatRoundResult CreateFromAzureString(String data)
        {
            CombatRoundResult roundResult = null;

            if (!String.IsNullOrEmpty(data))
            {
                roundResult = new CombatRoundResult();
                String[] armyStrings = data.Split('@');
                List<ICombatArmyRoundResult> armyResults = new List<ICombatArmyRoundResult>();
                for (int counter = 0; counter < armyStrings.Length; ++counter)
                {
                    armyResults.Add(CombatArmyRoundResult.CreateFromAzureString(armyStrings[counter]));
                }
                roundResult.ArmyResults = armyResults;
            }

            return roundResult;
        }
    }

    static public class CombatRoundResultHelperExtensionMethods
    {
        static public String EncodeToAzureString(this ICombatRoundResult roundResult)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ICombatArmyRoundResult armyResult in roundResult.ArmyResults)
            {
                builder.Append(armyResult.EncodeToAzureString());
                builder.Append('@');
            }

            return builder.ToString().TrimEnd('@');
        }
    }
}
