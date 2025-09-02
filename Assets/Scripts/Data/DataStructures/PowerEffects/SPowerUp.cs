using System;


namespace Data.DataStructures.PowerEffects
{
    [Serializable]
    public class SPowerUp : SPowerEffect
    {
        #region Members

        public float BonusPowerOrb      = 0;
        public int BonusRewardsRarety   = 0;

        #endregion


        #region Settings

        public override void SetLevel(int level)
        {
            base.SetLevel(level);
        }

        #endregion


        #region Infos

        public override string GetDescription()
        {
            var description = base.GetDescription();
            description = description.Replace("[BonusPowerOrb]", ((int)Math.Round(BonusPowerOrb * 100)).ToString() + "%");
            description = description.Replace("[BonusRewardsRarety]", BonusRewardsRarety.ToString());
            return description;
        }

        #endregion
    }

}