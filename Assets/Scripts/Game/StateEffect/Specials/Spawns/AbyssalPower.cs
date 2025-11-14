using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "AbyssalPower", menuName = "Game/StateEffects/SpecialEffects/AbyssalPower")]
    public class AbyssalPower : TickDamageEffect
    {
        #region Members


        #endregion


        #region Update

        public override int GetInt(EStateEffectProperty stateEffectProperty, int? stacks = null, EDamageCategory? damageCategory = null, EHitCategory? hitCategory = null, string specialCondition = "")
        {
            if (! GameManager.Exists || GameManager.IsGameOver)
            {
                return base.GetInt(stateEffectProperty, stacks: stacks, damageCategory: damageCategory, hitCategory: hitCategory, specialCondition: specialCondition);
            }

            if (stacks == null)
                stacks = GameManager.Instance.GetFirstEnemy(m_Caster.Team).StateHandler.GetStacks(EStateEffect.CorruptedPower);
            return base.GetInt(stateEffectProperty, stacks: stacks, damageCategory: damageCategory, hitCategory: hitCategory, specialCondition: specialCondition);
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            return base.GetDescription();
        }

        #endregion
    }
}