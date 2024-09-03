using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/StateEffects/DamageEffect")]
    public class DamageEffect : StateEffect
    {
        [Header("Damages")]
        [SerializeField] protected int      m_Damages;
        [SerializeField] protected int      m_EndDamages;
        [SerializeField] protected int      m_Heal;
        [SerializeField] protected int      m_EndHeal;
        [SerializeField] protected float    m_LifeSteal = 0f;

        protected float FinalLifeSteal => Mathf.Max(0f, m_LifeSteal + m_Controller.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal) - 1);

        /// <summary>
        /// Apply damages / Heal on end
        /// </summary>
        protected override void OnStart()
        {
            // hit
            var damages = m_Controller.Life.Hit(GetInt(EStateEffectProperty.Damages));

            // apply lifesteal (on caster)
            m_Caster.Life.Heal((int)Mathf.Round(damages * FinalLifeSteal));

            // apply heal
            m_Controller.Life.Heal(GetInt(EStateEffectProperty.Heal));

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
            var damages = GetInt(EStateEffectProperty.EndDamages);

            // add special bonus damages
            if (StateEffectName == EStateEffect.Burn.ToString())
                damages += m_Caster.StateHandler.GetInt(EStateEffectProperty.BonusBurnDamages);

            // hit
            damages = m_Controller.Life.Hit(damages);

            // apply lifesteal (on caster)
            m_Caster.Life.Heal((int)Mathf.Round(damages * FinalLifeSteal));

            // apply heal
            m_Controller.Life.Heal(GetInt(EStateEffectProperty.EndHeal));
        }

        #endregion
    }
}