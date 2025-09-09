using Data.DataStructures.PowerEffects;
using Enums;
using System;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "RuneData", menuName = "Game/Runes/Default")]
    public class RuneData : PowerEffectData<SRunePower>
    {
        #region Members

        protected override Type m_EnumType => typeof(ERune);
        public ERune Rune => Enum.TryParse(Name, out ERune rune) ? rune : ERune.None;

        #endregion


        #region Level

        public new RuneData Clone(int level = 0, bool destroy = false)
        {
            return (RuneData)base.Clone(level, destroy);
        }

        #endregion
    }
}