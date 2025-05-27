using Enums;
using System;
using System.Globalization;
using Tools;

namespace Data.DataStructures.SpellSubStructures
{
    public enum EOverridingType
    {
        Replacement,        // replace the base value by the overriding value
        Additive,           // add the overriding value to the base value
        Multiply,           // multiply the base value by the overriding value
    }

    [Serializable]
    public struct SOverridingData
    {
        public ESpellProperty           Property;
        public EOverridingType          OverridingType;
        public string                   Value;
        public SSpellPropertyScaling    SpellPropertyScaling;

        public float Get(float baseValue, int level)
        {
            if (! float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float overrideValue))
            {
                ErrorHandler.Error("Trying to get overrided scaled value of " + Property + " but provided value " + Value + " is not a float");
                return baseValue;
            }

            overrideValue = SpellPropertyScaling.Get(overrideValue, level);

            switch (OverridingType)
            {
                case EOverridingType.Replacement:
                    return overrideValue;

                case EOverridingType.Additive:
                    return overrideValue + baseValue;
                    
                case EOverridingType.Multiply:
                    return overrideValue * baseValue;

                default:
                    ErrorHandler.Warning("Unhandled case : " + OverridingType);
                    return baseValue;

            }
        }
    }
}