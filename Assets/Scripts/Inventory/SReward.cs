using System;


namespace Inventory
{
    public struct SReward
    {
        public const string METADATA_KEY_SPELL_TYPE = "SpellType";

        public Type RewardType;
        public string RewardName;
        public int Qty;

        public SReward(Type rewardType, string name, int count)
        {
            RewardType  = rewardType;
            RewardName  = name;
            Qty         = count;
        }

        public void AddQty(int qty)
        {
            Qty += qty;
        }
    }
}