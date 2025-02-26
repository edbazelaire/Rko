using Data;
using Enums;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect", menuName = "Game/StateEffects/Aura/SpellEffect")]
    public class SpellEffect : StateEffect
    {
        [Header("Spell Effects")]
        [SerializeField] protected ESpellEffectType         m_SpellEffectType;
        [SerializeField] protected List<ESpellType>         m_AllowedSpellTypes;
        [SerializeField] protected List<SpellData>          m_OnHits;
        [SerializeField] protected List<SStateEffectData>   m_AllyStateEffects;
        [SerializeField] protected List<SStateEffectData>   m_EnemyStateEffects;

        public ESpellEffectType         SpellEffectType     => m_SpellEffectType;
        public List<ESpellType>         AllowedSpellTypes   => m_AllowedSpellTypes;
        public List<SpellData>          OnHits              => m_OnHits;
        public List<SStateEffectData>   AllyStateEffects    => m_AllyStateEffects;
        public List<SStateEffectData>   EnemyStateEffects   => m_EnemyStateEffects;


        #region Authorisation

        public bool IsAllowed(ESpellType spellType, bool isAutoAttack)
        {
            if (m_SpellEffectType == ESpellEffectType.AutoAttack && !isAutoAttack)
                return false;

            if (m_SpellEffectType == ESpellEffectType.Spells && isAutoAttack)
                return false;

            if (m_AllowedSpellTypes.Count != 0 && ! m_AllowedSpellTypes.Contains(spellType))
                return false;

            return true;
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