using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "BlightHarvestStack", menuName = "Game/StateEffects/SpecialEffects/PowerUps/BlightHarvestStack")]
    public class BlightHarvestStack : StateEffect
    {
        #region Members

        [SerializeField] float          m_PercHpDamage;
        [SerializeField] SScalingStat   m_CursedStacks;

        #endregion


        #region Stacks

        public override int RecalculateStacks(int stacks, Controller caster, Controller targetController)
        {
            int cursedStacks = (int)Math.Round(m_CursedStacks.GetValue());
            if (cursedStacks > targetController.StateHandler.GetStacks(EStateEffect.Cursed))
                return 0;

            // consume stacks of Cursed
            targetController.StateHandler.RemoveStateEffect(EStateEffect.Cursed.ToString(), consume: true, maxStacks: cursedStacks);
            // hit for % hp
            int damages = (int)Math.Round(caster.Life.MaxHp.Value * m_PercHpDamage);
            targetController.Life.Hit(damages, caster.PlayerId, "BlightHarvest", EDamageCategory.Physical, EHitCategory.True, true);
            Debug.Log("BlightHarvest - APPLYING : " + damages);
            // add a stack to the effect
            caster.StateHandler.AddStateEffect("BlightHarvest", caster, m_Level, "BlightHarvest");
            Debug.Log("BlightHarvest - stacks : " + caster.StateHandler.GetStacks("BlightHarvest"));

            // exit with 0 stacks (to skip)
            return 0;
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[PercHpDamage]", (100*m_PercHpDamage).ToString("0") + "%");
            description = description.Replace("[CursedStacks]", m_CursedStacks.GetValue().ToString("0"));
            return description;
        }

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_CursedStacks.SetLevel(level);
        }

        #endregion
    }
}