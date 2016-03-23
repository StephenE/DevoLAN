using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peril.Api.Repository.Azure.Model
{
    public class CombatArmyRoundResult : ICombatArmyRoundResult
    {
        public CombatArmyRoundResult(Guid regionId, String userId, IEnumerable<UInt32> results, UInt32 troopsLost)
        {
            OriginRegionId = regionId;
            OwnerUserId = userId;
            RolledResults = results;
            TroopsLost = troopsLost;
        }

        private CombatArmyRoundResult()
        {

        }

        public Guid OriginRegionId { get; set; }

        public String OwnerUserId { get; set; }

        public IEnumerable<UInt32> RolledResults { get; set; }

        public UInt32 TroopsLost { get; set; }

        static public CombatArmyRoundResult CreateFromAzureString(String data)
        {
            CombatArmyRoundResult armyResult = null;

            if (!String.IsNullOrEmpty(data))
            {
                armyResult = new CombatArmyRoundResult();
                String[] armyStrings = data.Split('#');
                if (armyStrings.Length >= 3)
                {
                    armyResult.OriginRegionId = Guid.Parse(armyStrings[0]);
                    armyResult.OwnerUserId = armyStrings[1];
                    armyResult.TroopsLost = UInt32.Parse(armyStrings[2]);
                    List<UInt32> rolledResults = new List<UInt32>();
                    for(int counter = 3; counter < armyStrings.Length; ++counter)
                    {
                        rolledResults.Add(UInt32.Parse(armyStrings[counter]));
                    }
                    armyResult.RolledResults = rolledResults;
                }
            }

            return armyResult;
        }
    }

    static public class CombatArmyRoundResultHelperExtensionMethods
    {
        static public String EncodeToAzureString(this ICombatArmyRoundResult armyResult)
        {
            if (armyResult.OwnerUserId.Contains('@') || armyResult.OwnerUserId.Contains('#') || armyResult.OwnerUserId.Contains(';'))
            {
                throw new InvalidOperationException("OwnerUserId contains unsupported characters");
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(armyResult.OriginRegionId);
            builder.Append('#');
            builder.Append(armyResult.OwnerUserId);
            builder.Append('#');
            builder.Append((Int32)armyResult.TroopsLost);
            foreach (UInt32 diceRoll in armyResult.RolledResults)
            {
                builder.Append('#');
                builder.Append(diceRoll);
            }
            return builder.ToString();
        }
    }
}
