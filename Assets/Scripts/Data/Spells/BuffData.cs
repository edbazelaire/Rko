using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Game/Spells/Buff")]
    public class BuffData : SpellData
    {
        #region Members

        public override ESpellType SpellType => ESpellType.Buff;

        public override float Duration => (float)Math.Round(m_Duration * GetSpellLevelFactor(ESpellProperty.Duration));

        #endregion


        #region State Effect

        public StateEffect GetStateEffect()
        {
            return SpellLoader.GetStateEffect(Name, Level);
        }

        #endregion


        #region Infos & Description

        public override Dictionary<string, object> GetInfos()
        {
            var infoDict = base.GetInfos();
            var stateEffect = GetStateEffect();
            var stateEffectInfos = stateEffect.GetInfos();

            foreach (var item in stateEffectInfos)
            {
                if (infoDict.ContainsKey(item.Key))
                    infoDict[item.Key] = item.Value; 
                else 
                    infoDict.Add(item.Key, item.Value);
            }

            Destroy(stateEffect);

            return infoDict;
        }

        public override string GetDescription()
        {
            if (m_Description == "")
                return GetStateEffect().GetDescription();

            return base.GetDescription();
        }

        #endregion
    }
}