using Data.DataStructures;
using Data.DataStructures.CharacterSubStructures;
using Data.DataStructures.PowerEffects;
using Enums;
using Game.Loaders;
using MyBox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using Unity.VisualScripting;
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