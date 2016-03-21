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
            CombatArmy army = new CombatArmy();
            return army;
        }
    }

    static public class CombatHelperExtensionMethods
    {
        static public String EncodeToAzureString(this ICombatArmy army)
        {
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
