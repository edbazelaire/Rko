using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/StateEffects/DamageEffect")]
    public class DamageEffect : StateEffect
    {
        [Header("Damage")]
        [SerializeField] protected bool     m_IsTrueDamage  = false;
        [SerializeField] protected int      m_Damage        = 0;
        [SerializeField] protected int      m_EndDamage     = 0;
        [SerializeField] protected int      m_Heal          = 0;
        [SerializeField] protected int      m_EndHeal       = 0;
        [SerializeField] protected float    m_LifeSteal     = 0f;

        protected float FinalLifeSteal => Mathf.Max(0f, m_LifeSteal + m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal, m_Controller) - 1);

        /// <summary>
        /// Apply damages / Heal on end
        /// </summary>
        protected override void OnStart()
        {
            // hit
            var damages = m_Controller.Life.Hit(GetInt(EStateEffectProperty.Damage), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct, ignoreRes: m_IsTrueDamage);

            // apply lifesteal (on caster)
            m_Caster.Life.Heal((int)Mathf.Round(damages * FinalLifeSteal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);

            // apply heal
            m_Controller.Life.Heal(GetInt(EStateEffectProperty.Heal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);

            base.OnStart();
        }

        /// <summary>
        /// Apply damages / Heal on end
        /// </summary>
        public override void End()
        {
            ApplyEndHits();
            base.End();
        }


        #region Damages 

        protected virtual void ApplyEndHits()
        {
            // calculate spell damages
            var damages = GetInt(EStateEffectProperty.EndDamage);

            // add special bonus damages
            if (StateEffectName == EStateEffect.Burn.ToString())
                damages += m_Caster.StateHandler.GetInt(EStateEffectProperty.BonusBurnDamage);

            // hit
            damages = m_Controller.Life.Hit(damages, m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct, ignoreRes: m_IsTrueDamage);

            // apply lifesteal (on caster)
            m_Caster.Life.Heal((int)Mathf.Round(damages * FinalLifeSteal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);

            // apply heal
            m_Controller.Life.Heal(GetInt(EStateEffectProperty.EndHeal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);
        }

        #endregion
    }
}