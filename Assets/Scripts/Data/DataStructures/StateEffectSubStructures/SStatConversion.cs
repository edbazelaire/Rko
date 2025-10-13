using Enums;
using Game.Spells;
using System;


namespace Data.DataStructures.StateEffectSubStructures
{
    /// <summary>
    /// Convert the value of a stat, to another
    /// </summary>
    [Serializable]
    public struct SStatConversion
    {
        // TODO : REMOVE    ==================================
        public EStateEffectProperty ExpectedStat;
        // TODO : REMOVE    ==================================

        public SBonusStats ToStat;
        public SBonusStats OriginalStatScaling;

        public readonly bool HasStat(EStateEffectProperty stateEffectProperty, EDamageCategory? damageCategory = null, EHitCategory? hitCategory = null, string specialConditions = "") => ToStat.StateEffectProperty == stateEffectProperty && ToStat.CheckConditions(damageCategory, hitCategory, specialConditions);
        public readonly float Get(Controller controller, int level, int stacks)
        {
            var baseValue = controller.StateHandler.GetFloat(
                OriginalStatScaling.StateEffectProperty,
                damageCategory:     OriginalStatScaling.DamageCategories.Count != 1 ?   null : OriginalStatScaling.DamageCategories[0], 
                hitCategory:        OriginalStatScaling.HitCategories.Count != 1 ?      null : OriginalStatScaling.HitCategories[0], 
                ignoreConversion: true);

            return baseValue * OriginalStatScaling.Get(level, stacks);
        }

        public string GetDescription(int level, int stacks)
        {
            return $"Convert {Math.Round(OriginalStatScaling.Get(level, stacks) * 100)}% of your {OriginalStatScaling.GetPrettyName()} into {ToStat.GetPrettyName()}";
        }
    }
}