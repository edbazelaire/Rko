using Assets.Scripts.Data.DataStructures;
using Data;
using Data.DataStructures.SpellSubStructures;
using Enums;
using Google.Apis.Sheets.v4.Data;
using MyBox;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect", menuName = "Game/StateEffects/Aura/SpellEffect")]
    public class SpellEffect : StateEffect
    {
        #region Members

        /// <summary> List of effects that are applied at the root SpellData (in the SpellHandler) </summary>
        List<ESpellProperty> PRE_APPLIED_EFFECTS = new List<ESpellProperty>() {
            ESpellProperty.Charges,
            ESpellProperty.AnimationTimer,
            ESpellProperty.Cooldown,
        };

        [Header("Spell Effects")]
        [SerializeField] protected int                      m_Frequency = 1;
        [SerializeField] protected List<string>             m_AllowedSpells;
        [SerializeField] protected ESpellEffectType         m_SpellEffectType;
        [SerializeField] protected List<ESpellType>         m_AllowedSpellTypes;
        [SerializeField] protected List<SpellData>          m_OnHits;
        [SerializeField] protected List<SStateEffectData>   m_AllyStateEffects;
        [SerializeField] protected List<SStateEffectData>   m_EnemyStateEffects;
        [SerializeField] protected List<SOverridingData>    m_SpellOverridingData;
        [SerializeField] protected List<SpellPrefabSpawn>   m_SpellGFXOverrides;

        public int                      Frequency           => m_Frequency;
        public ESpellEffectType         SpellEffectType     => m_SpellEffectType;
        public List<ESpellType>         AllowedSpellTypes   => m_AllowedSpellTypes;
        public List<SpellData>          OnHits              => m_OnHits;
        public List<SStateEffectData>   AllyStateEffects    => m_AllyStateEffects;
        public List<SStateEffectData>   EnemyStateEffects   => m_EnemyStateEffects;

        // =======================================================================================
        // Local Data
        int m_FrequencyCounter = 0;

        #endregion


        #region Apply Effect

        /// <summary>
        /// Apply (if possible) the effects of this Spell Effect on the provided spell data
        /// </summary>
        /// <param name="spellData"></param>
        /// <returns></returns>
        public void Apply(ref SpellData spellData, bool isAutoAttack)
        {
            if (!IsAllowed(spellData, isAutoAttack))
                return;

            if (!CheckFrequency())
                return;

            spellData.OnHit.AddRange(OnHits);
            spellData.AllyStateEffects.AddRange(AllyStateEffects);
            spellData.EnemyStateEffects.AddRange(EnemyStateEffects);

            // apply overrides 
            if (! m_SpellOverridingData.IsNullOrEmpty())
            {
                List<SOverridingData> overridingData = m_SpellOverridingData.Where(temp => !PRE_APPLIED_EFFECTS.Contains(temp.Property)).ToList();
                spellData.AddOverridingData(overridingData, m_Level);
            }

            // override data of the spell
            ApplySpellDataOverrides(ref spellData);

            // override GFX of the spell
            ApplySpellGFXOverrides(ref spellData);
        }

        bool IsAllowed(SpellData spellData, bool isAutoAttack)
        {
            // if works for specific spells : check that provided spell data is one of them
            if (! m_AllowedSpells.IsNullOrEmpty())
                return m_AllowedSpells.Contains(spellData.Name);

            // CHECK : is AutoAttack
            if (m_SpellEffectType == ESpellEffectType.AutoAttack && !isAutoAttack)
                return false;

            // CHECK : is spell
            if (m_SpellEffectType == ESpellEffectType.Spells && isAutoAttack)
                return false;

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

        /// <summary>
        /// Override the Data of the spell
        /// </summary>
        /// <param name="spellData"></param>
        protected virtual void ApplySpellDataOverrides(ref SpellData spellData)
        {
            if (m_SpellOverridingData.IsNullOrEmpty())
                return;

            List<SOverridingData> overridingData = m_SpellOverridingData.Where(temp => !PRE_APPLIED_EFFECTS.Contains(temp.Property)).ToList();
            spellData.AddOverridingData(overridingData, m_Level);
        }
           
        /// <summary>
        /// Add / Replace GFXs of the spell
        /// </summary>
        /// <param name="spellData"></param>
        protected virtual void ApplySpellGFXOverrides(ref SpellData spellData)
        {
            if (m_SpellGFXOverrides.IsNullOrEmpty())
                return;

            spellData.SpellEventActions.AddRange(m_SpellGFXOverrides);
        }

        #endregion


        #region Checkers


        #endregion


        #region Listeners

        protected override void OnRefreshed(int stacks)
        {
            base.OnRefreshed(stacks);

            if (m_SpellOverridingData.IsNullOrEmpty())
                return;

            List<SOverridingData> overridingData = m_SpellOverridingData.Where(temp => PRE_APPLIED_EFFECTS.Contains(temp.Property)).ToList();
            if (m_SpellOverridingData.IsNullOrEmpty())
                return;

            // check all spells in SpellHandler to see compatibles and pre-apply some effects
            for (int i = 0; i < m_Controller.SpellHandler.Spells.Count(); i++)
            {
                if (!IsAllowed(m_Controller.SpellHandler.GetSpellDataAtIndex(i), isAutoAttack: i == 0))
                    continue;

                // override properties of spell data
                m_Controller.SpellHandler.OverrideSpellDataAtIndex(i, overridingData, m_Level);
            }
        }

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

            for (int i = 0; i < m_AllyStateEffects.Count; i++)
            {
                var effect = m_AllyStateEffects[i];
                effect.SetLevel(level);
                m_AllyStateEffects[i] = effect;
            }

            for (int i = 0; i < m_EnemyStateEffects.Count; i++)
            {
                var effect = m_EnemyStateEffects[i];
                effect.SetLevel(level);
                m_EnemyStateEffects[i] = effect;
            }
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[Frequency]", m_Frequency.ToString());
            return description;
        }

        protected override void ReplaceSubStateEffects(ref string description)
        {
            var effects = m_EnemyStateEffects;
            effects.AddRange(m_AllyStateEffects);
            description = TextHandler.ReplaceSubStateEffects(description, effects);
        }

        #endregion
    }
}