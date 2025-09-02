using Data.DataStructures.PowerEffects;
using Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "PowerUpData", menuName = "Game/Effects/PowerUpData/Default")]
    public class PowerUpData : PowerEffectData<SPowerUp>
    {
        #region Members

        public List<EPowerUpTag> PowerUpTags;

        #endregion


        #region Level

        public new PowerUpData Clone(int level = 0, bool destroy = false)
        {
            return (PowerUpData)base.Clone(level, destroy);
        }

        #endregion
    }
}