using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Core;

namespace Peril.Api.Repository.Azure.Model
{
    public class CombatArmy : ICombatArmy
    {
        public CombatArmy(Guid regionId, String owner, CombatArmyMode mode, UInt32 numberOfTroops)
        {
            OriginRegionId = regionId;
            OwnerUserId = owner;
            ArmyMode = mode;
            NumberOfTroops = numberOfTroops;
        }

        private CombatArmy()
        {

        }

        public Guid OriginRegionId { get; set; }
        public String OwnerUserId { get; set; }
        public CombatArmyMode ArmyMode { get; set; }
        public UInt32 NumberOfTroops { get; set; }

        static public CombatArmy CreateFromAzureString(String data)
        {
            CombatArmy army = null;

            if (!String.IsNullOrEmpty(data))
            {
                army = new CombatArmy();
                String[] armyStrings = data.Split('#');
                if(armyStrings.Length == 4)
                {
                    army.OriginRegionId = Guid.Parse(armyStrings[0]);
                    army.OwnerUserId = armyStrings[1];
                    army.ArmyMode = (CombatArmyMode)Enum.Parse(typeof(CombatArmyMode), armyStrings[2]);
                    army.NumberOfTroops = UInt32.Parse(armyStrings[3]);
                }
            }

            return army;
        }
    }

    static public class CombatHelperExtensionMethods
    {
        static public String EncodeToAzureString(this ICombatArmy army)
        {
            if(army.OwnerUserId.Contains('@') || army.OwnerUserId.Contains('#'))
            {
                throw new InvalidOperationException("OwnerUserId contains unsupported characters");
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(army.OriginRegionId);
            builder.Append('#');
            builder.Append(army.OwnerUserId);
            builder.Append('#');
            builder.Append((Int32)army.ArmyMode);
            builder.Append('#');
            builder.Append(army.NumberOfTroops);
            return builder.ToString();
        }
    }
}
