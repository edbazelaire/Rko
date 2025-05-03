using Enums;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "StateEffect", menuName = "Game/StateEffects/Aura/MissingLifeAura")]
    public class MissingLifeAura : StateEffect
    {
        #region Members

        [Header("Missing Life")]
        [SerializeField, Tooltip("Bonus applied depending of percentage of health loss")] 
        protected float                m_MissingLifeFactor;
        [SerializeField, Tooltip("Target to check when calculating missing life")]
        protected EStateEffectTarget   m_TargetMissingLife = EStateEffectTarget.Self;
        
        #endregion


        #region Data Accessors

        public override int GetInt(EStateEffectProperty property)
        {
            if (m_Controller == null)
                return (int)Mathf.Round(ApplyMissingLifeFactor(base.GetInt(property), 0, 1));

            return (int)Mathf.Round(GetFloat(property));
        }

        public override float GetFloat(EStateEffectProperty property, bool ignoreConversion = false)
        {
            if (m_Controller == null)
                return ApplyMissingLifeFactor(base.GetFloat(property, ignoreConversion), 0, 1);

            var controller = GetTarget(m_Caster, null);
            return ApplyMissingLifeFactor(base.GetFloat(property, ignoreConversion), controller.Life.Hp.Value, controller.Life.MaxHp.Value);
        }

        #endregion


        #region Private Method

        float ApplyMissingLifeFactor(float baseValue, int hp, int maxHp)
        {
            return m_MissingLifeFactor * (1 - (hp / maxHp)) * baseValue;
        }

        public Controller GetTarget(Controller caster, Controller target)
        {
            if (!GameManager.Exists)
                return null;

            switch (m_TargetMissingLife)
            {
                case EStateEffectTarget.None:
                    ErrorHandler.Error("no StateEffectTarget provided");
                    return null;

                case EStateEffectTarget.Self:
                    return caster;

                case EStateEffectTarget.Target:
                    if (target == null)
                        ErrorHandler.Error("Provided target controller is null");
                    return target;

                case EStateEffectTarget.Ally:
                    return GameManager.Instance.GetFirstAlly(caster.Team, caster.PlayerId);

                case EStateEffectTarget.Enemy:
                    return GameManager.Instance.GetFirstEnemy(caster.Team);

                default:
                    ErrorHandler.Warning("Unahandled case : " + m_TargetMissingLife);
                    return null;
            }
        }

        #endregion


        #region Infos

        /// <summary>
        /// Handle the info management of special cases 
        /// </summary>
        /// <param name="infosDict"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected override bool GetSpecialPropertiesInfos(ref Dictionary<string, object> infosDict, EStateEffectProperty property)
        {
            switch (property)
            {
                // skip this one
                case EStateEffectProperty.MissingLifeFactor:
                    return true;

                default:
                    return base.GetSpecialPropertiesInfos(ref infosDict, property);
            }
        }

        #endregion

    }
}