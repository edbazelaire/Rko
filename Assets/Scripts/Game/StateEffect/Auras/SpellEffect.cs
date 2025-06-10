using Data;
using Data.DataStructures.SpellSubStructures;
using Enums;
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
        /// <summary> List of effects that are applied at the root SpellData (in the SpellHandler) </summary>
        List<ESpellProperty> PRE_APPLIED_EFFECTS = new List<ESpellProperty>() {
            ESpellProperty.Charges,
            ESpellProperty.AnimationTimer,
            ESpellProperty.Cooldown,
        };

        [Header("Spell Effects")]
        [SerializeField] protected List<string>             m_AllowedSpells;
        [SerializeField] protected ESpellEffectType         m_SpellEffectType;
        [SerializeField] protected List<ESpellType>         m_AllowedSpellTypes;
        [SerializeField] protected List<SpellData>          m_OnHits;
        [SerializeField] protected List<SStateEffectData>   m_AllyStateEffects;
        [SerializeField] protected List<SStateEffectData>   m_EnemyStateEffects;
        [SerializeField] protected List<SOverridingData>    m_SpellOverridingData;

        public ESpellEffectType         SpellEffectType     => m_SpellEffectType;
        public List<ESpellType>         AllowedSpellTypes   => m_AllowedSpellTypes;
        public List<SpellData>          OnHits              => m_OnHits;
        public List<SStateEffectData>   AllyStateEffects    => m_AllyStateEffects;
        public List<SStateEffectData>   EnemyStateEffects   => m_EnemyStateEffects;


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

            spellData.OnHit.AddRange(OnHits);
            spellData.AllyStateEffects.AddRange(AllyStateEffects);
            spellData.EnemyStateEffects.AddRange(EnemyStateEffects);

            // apply overrides
            if (! m_SpellOverridingData.IsNullOrEmpty())
            {
                List<SOverridingData> overridingData = m_SpellOverridingData.Where(temp => !PRE_APPLIED_EFFECTS.Contains(temp.Property)).ToList();
                spellData.AddOverridingData(overridingData, m_Level);
            }
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


        #region Description

        protected override void ReplaceSubStateEffects(ref string description)
        {
            var effects = m_EnemyStateEffects;
            effects.AddRange(m_AllyStateEffects);
            description = TextHandler.ReplaceSubStateEffects(description, effects);
        }

        #endregion
    }
}