using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Enums;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using MyBox;
using Data.DataStructures.StateEffectSubStructures;


namespace Data.DataStructures.CharacterSubStructures
{
    [Serializable]
    public struct SCharacterStatScaling : INetworkSerializable
    {
        public EStateEffectProperty             StateEffectProperty;
        public float                            BaseValue;
        public float                            BonusValue;
        public float                            ScalingFactor;
        public List<EDamageCategory>            DamageCategories;
        public List<EHitCategory>               HitCategories;
        public List<string>                     SpecialConditions;
        public List<SStateEffectStackFactor>    StateEffectStackFactors;

        public readonly EScalingDirection ScalingDirection => ScalingFactor > 0 ? EScalingDirection.Up : (ScalingFactor < 0 ? EScalingDirection.Down : EScalingDirection.None);

        public SCharacterStatScaling(EStateEffectProperty stateEffectProperty, float baseValue, float bonusValue, float scalingFactor = 0.1f, List<EDamageCategory> damageCategories = default, List<EHitCategory> hitCategories = default, List<string> specialConditions = null, List<SStateEffectStackFactor> stateEffectStackFactors = default)
        {
            StateEffectProperty     = stateEffectProperty;
            BaseValue               = baseValue;
            BonusValue              = bonusValue;
            ScalingFactor           = scalingFactor;
            DamageCategories        = damageCategories  ?? new List<EDamageCategory>();
            HitCategories           = hitCategories     ?? new List<EHitCategory>();
            SpecialConditions       = specialConditions ?? new List<string>();
            StateEffectStackFactors = stateEffectStackFactors != default ? stateEffectStackFactors : new List<SStateEffectStackFactor>();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StateEffectProperty);
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref BonusValue);
            serializer.SerializeValue(ref ScalingFactor);

            // -- Damage Categories
            int length = DamageCategories != null ? DamageCategories.Count : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                DamageCategories = new List<EDamageCategory>(length);
            }
            for (int i = 0; i < length; i++)
            {
                var temp = DamageCategories[i];
                serializer.SerializeValue(ref temp);
                if (serializer.IsReader)
                {
                    DamageCategories.Add(temp);
                }
            }

            // -- Hit Categories
            length = HitCategories != null ? HitCategories.Count : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                HitCategories = new List<EHitCategory>(length);
            }
            for (int i = 0; i < length; i++)
            {
                var temp = HitCategories[i];
                serializer.SerializeValue(ref temp);
                if (serializer.IsReader)
                {
                    HitCategories.Add(temp);
                }
            }

            // -- SPECIAL CONDITIONS
            length = SpecialConditions != null ? SpecialConditions.Count : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                SpecialConditions = new List<string>(length);
            }

            for (int i = 0; i < length; i++)
            {
                FixedString64Bytes temp = new FixedString64Bytes();

                if (!serializer.IsReader)
                {
                    temp = new FixedString64Bytes(SpecialConditions[i]);
                }

                serializer.SerializeValue(ref temp);

                if (serializer.IsReader)
                {
                    SpecialConditions.Add(temp.ToString());
                }
            }

            // -- STATE EFFECT STACKS FACTOR
            length = StateEffectStackFactors != null ? StateEffectStackFactors.Count : 0;
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                StateEffectStackFactors = new List<SStateEffectStackFactor>();
            }
            for (int i = 0; i < length; i++)
            {
                StateEffectStackFactors[i].NetworkSerialize(serializer);
            }
        }

        /// <summary>
        /// Return a SCharacterStatScaling but the final value is set as fixed bonus value
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public SCharacterStatScaling AsBonus(int level)
        {
            // set BonusValue as the total value for the level
            BonusValue = GetValue(level);

            // reset scaling values
            BaseValue = 0f;
            ScalingFactor = 0f;

            return this;
        }

        public float GetValue(int level, Controller controller = null, Controller targetController = null)
        {
            return BonusValue
                + BaseValue * Mathf.Pow(1 + ScalingFactor, Mathf.Max(0, level - 1))
                + GetStateEffectStackBonus(level, controller, targetController);
        }

        public float GetDefaultValue(int level)
        {
            float value = BonusValue + BaseValue * Mathf.Pow(1 + ScalingFactor, level - 1);

            foreach (var stateEffectStackFactor in StateEffectStackFactors)
            {
                value += stateEffectStackFactor.GetBonusValue(level, nStacks: 1);
            }

            return value;
        }

        float GetStateEffectStackBonus(int level, Controller controller, Controller targetController)
        {
            if (controller == null || StateEffectStackFactors == null)
                return 0.0f;

            float value = 0.0f;
            foreach (var stateEffectStackFactor in StateEffectStackFactors)
            {
                value += stateEffectStackFactor.GetBonusValue(level, controller, targetController);
            }

            return value;
        }

        public bool HasDamageCategory(EDamageCategory? damageCategory)
        {
            // This effect has no specific damage category - return true
            if (DamageCategories.IsNullOrEmpty())
                return true;

            // No damage category for the requested value - return true
            if (damageCategory == null)
                return true;

            return DamageCategories.Contains(damageCategory.Value);
        }

        public bool HasHitCategory(EHitCategory? hitCategory)
        {
            // This effect has no specific damage category - return true
            if (HitCategories.IsNullOrEmpty())
                return true;

            // No damage category for the requested value - return true
            if (hitCategory == null)
                return true;

            return HitCategories.Contains(hitCategory.Value);
        }

        public bool HasSpecialCondition(string specialCondition)
        {
            // UNIQUE - can only apply IF has the special condition allowed
            if (SBonusStats.IsUnique(specialCondition, out string formatedString))
                return SpecialConditions.Contains(formatedString);

            if (SpecialConditions.IsNullOrEmpty())
                return true;

            return SpecialConditions.Contains(specialCondition);
        }
    }

}