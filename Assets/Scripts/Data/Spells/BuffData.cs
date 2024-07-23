using Enums;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Game/Spells/Buff")]
    public class BuffData : SpellData
    {
        public override ESpellType SpellType => ESpellType.Buff;

        public override float Duration => (float)Math.Round(m_Duration * GetSpellLevelFactor(ESpellProperty.Duration));


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
            var stateEffectInfos = GetStateEffect().GetInfos();

            foreach (var item in stateEffectInfos)
            {
                if (infoDict.ContainsKey(item.Key))
                    infoDict[item.Key] = item.Value; 
                else 
                    infoDict.Add(item.Key, item.Value);
            }
            
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