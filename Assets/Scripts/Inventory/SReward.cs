using Enums;
using System;


namespace Inventory
{
    public struct SReward
    {
        public const string METADATA_KEY_SPELL_TYPE = "SpellType";

        public Type     RewardType;
        public string   RewardName;
        public int      Qty;

        public SReward(Type rewardType, string name, int count)
        {
            RewardType  = rewardType;
            RewardName  = name;
            Qty         = count;
        }

        public SReward(SPowerOrb powerOrb)
        {
            RewardType  = typeof(EPowerOrb);
            RewardName  = powerOrb.ToName();
            Qty         = 1;
        }

        public void AddQty(int qty)
        {
            Qty += qty;
        }
    }
}