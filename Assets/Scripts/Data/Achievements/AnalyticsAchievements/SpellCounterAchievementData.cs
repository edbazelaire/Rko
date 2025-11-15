using Assets.Scripts.Game;
using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "SpellCounterAchievementData", menuName = "Game/Achievements/Analytics/SpellCounter")]
    public class SpellCounterAchievementData : DefaultAchievementData
    {
        #region Members

        [SerializeField]
        protected List<ESpell>          m_Spells;
        [SerializeField]
        protected List<EStateEffect>    m_StateEffects;

        public List<ESpell> Spells => m_Spells;
        public List<EStateEffect> StateEffects => m_StateEffects;

        #endregion


        #region Check

        /// <summary>
        /// Use end game analytics data (GameAnalyticsManager) to check end game achievements
        /// </summary>
        /// <param name="spellHitSummary"></param>
        /// <param name="specialValues"></param>
        /// <param name="spellHitTypeDatas"></param>
        public bool Check(List<SSpellHitTypeData> spellHitTypeDatas, bool save = false)
        {
            List<string> allEffects = new();
            if (m_Spells != null && m_Spells.Count > 0 )
                allEffects.AddRange(m_Spells.Select(t => t.ToString()).ToList());
            if (m_StateEffects != null && m_StateEffects.Count > 0 )
                allEffects.AddRange(m_StateEffects.Select(t => t.ToString()).ToList());

            if (allEffects.Count == 0)
            {
                ErrorHandler.Warning("SpellCounterAchievementData ("+name+") has no Spell or StateEffect to check");
                return false;
            }

            int value = 0;
            foreach (string effect in allEffects)
            {
                value += spellHitTypeDatas.Sum(t => t.SpellName == effect ? t.Counter : 0);
            }

            // no value to increment - no need to trigger save
            if (value == 0)
                return false;

            if (value < 0)
            {
                ErrorHandler.Warning("Found value (" + value + ") < 0");
                return false;
            }

            Increase(value, save: save);
            return true;
        }

        #endregion
    }
}