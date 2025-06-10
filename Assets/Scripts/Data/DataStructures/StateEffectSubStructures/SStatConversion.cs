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
        public EStateEffectProperty ExpectedStat;
        public SBonusStats OriginalStatScaling;

        public readonly bool HasStat(EStateEffectProperty stateEffectProperty) => ExpectedStat == stateEffectProperty;
        public readonly float Get(Controller controller, int level, int stacks)
        {
            var baseValue = controller.StateHandler.GetFloat(OriginalStatScaling.StateEffectProperty, ignoreConversion: true);
            return baseValue * OriginalStatScaling.Get(level, stacks);
        }
    }
}