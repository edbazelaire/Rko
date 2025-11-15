using Assets.Scripts.Data.DataStructures;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using MyBox;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "StateEffectMod", menuName = "Game/StateEffects/Aura/StateEffectMod")]
    public class StateEffectMod : StateEffect
    {
        #region Members

        [Header("State Effect Mod")]
        [SerializeField] protected int                          m_Frequency = 1;
        [SerializeField] protected List<string>                 m_AllowedStateEffects;
        [SerializeField] protected List<SBonusStatsOverride>    m_BonusStatsOverride;
        [SerializeField] protected List<StateEffectSpawnGFX>    m_GFXOverrides;

        public int                      Frequency               => m_Frequency;
        public List<SBonusStatsOverride> BonusStatsOverride     => m_BonusStatsOverride;

        // =======================================================================================
        // Local Data
        int m_FrequencyCounter = 0;

        #endregion


        #region Apply Effect


        /// <summary>
        /// Override the Data of the spell
        /// </summary>
        /// <param name="spellData"></param>
        protected virtual void ApplyOverrides(ref StateEffect stateEffect)
        {
        }
           
        /// <summary>
        /// Add / Replace GFXs of the spell
        /// </summary>
        /// <param name="spellData"></param>
        protected virtual void ApplySpellGFXOverrides(ref StateEffect stateEffect)
        {
            if (m_GFXOverrides.IsNullOrEmpty())
                return;

            stateEffect.GfxEffects.AddRange(m_GFXOverrides);
        }

        #endregion


        #region Checkers

        public bool CheckCanBeApplied(StateEffect stateEffect)
        {
            return IsAllowed(stateEffect) && CheckFrequency();
        }

        bool IsAllowed(StateEffect stateEffect)
        {
            // if works for specific spells : check that provided spell data is one of them
            if (!m_AllowedStateEffects.IsNullOrEmpty())
                return m_AllowedStateEffects.Contains(stateEffect.StateEffectName);

            return true;
        }

        bool CheckFrequency()
        {
            if (m_Frequency <= 1)
                return true;

            m_FrequencyCounter++;
            if (m_FrequencyCounter < m_Frequency)
                return false;

            m_FrequencyCounter = 0;
            return true;
        }



        #endregion


        #region Listeners

        protected override void OnRemoved(int stacks)
        {
            // TODO ---------------------------
            Debug.LogWarning("TODO : SpellEffect.OnRemoved()");
            // TODO ---------------------------
        }

        #endregion


        #region Level

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            return TextHandler.ReplaceStateEffectOverridingProperties(base.GetDescription(), m_BonusStatsOverride, m_Level);
        }

        #endregion
    }
}