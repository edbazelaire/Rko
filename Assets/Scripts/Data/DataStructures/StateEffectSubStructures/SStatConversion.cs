using Enums;
using Game.Spells;
using System;
using Tools;


namespace Data.DataStructures.StateEffectSubStructures
{
    /// <summary>
    /// Convert the value of a stat, to another
    /// </summary>
    [Serializable]
    public struct SStatConversion
    {
        public SBonusStats ToStat;
        public SBonusStats OriginalStatScaling;

        public readonly bool HasStat(EStateEffectProperty stateEffectProperty, EDamageCategory? damageCategory = null, EHitCategory? hitCategory = null, string specialCondition = "") => ToStat.StateEffectProperty == stateEffectProperty && ToStat.CheckConditions(damageCategory, hitCategory, specialCondition);
        public readonly float Get(Controller controller, int level, int stacks)
        {
            var baseValue = controller.StateHandler.GetFloat(
                OriginalStatScaling.StateEffectProperty,
                damageCategory:     OriginalStatScaling.DamageCategories.Count != 1 ?   null : OriginalStatScaling.DamageCategories[0], 
                hitCategory:        OriginalStatScaling.HitCategories.Count != 1 ?      null : OriginalStatScaling.HitCategories[0], 
                ignoreConversion: false);

            float finalValue = baseValue * OriginalStatScaling.Get(level, stacks);
            if (ToStat.BaseValue != 0)
                finalValue *= ToStat.Get(level, stacks);

            ErrorHandler.Log($"         - Stat Conversion : {OriginalStatScaling.StateEffectProperty} ({baseValue}) --> {ToStat.StateEffectProperty} ({finalValue})", ELogTag.StatConversion);

            return finalValue;
        }

        public string GetDescription(int level, int stacks)
        {
            string originalConversionString = $"your {OriginalStatScaling.GetPrettyName()}{TextHandler.FormatIcon(OriginalStatScaling.GetPropertyName())}";
            if (OriginalStatScaling.BaseValue != 1)
                originalConversionString = $"{Math.Round(OriginalStatScaling.Get(level, stacks) * 100)}% of " + originalConversionString;

            string toConversion = ToStat.GetPrettyName() + TextHandler.FormatIcon(ToStat.GetPropertyName());
            if (ToStat.BaseValue != 0)
                toConversion = TextHandler.FormatPropertyValue(ToStat.Get(level, stacks), ToStat.GetPropertyName(), ToStat.ScalingDirection) + " " + toConversion;

            return $"Convert {originalConversionString} into {toConversion}";
        }
    }
}