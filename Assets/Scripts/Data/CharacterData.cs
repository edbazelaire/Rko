using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct SCharacterStatScaling : INetworkSerializable
    {
        public EStateEffectProperty             StateEffectProperty;
        public float                            BaseValue;
        public float                            BonusValue;
        public float                            ScalingFactor;
        public List<SStateEffectStackFactor>    StateEffectStackFactors;

        public SCharacterStatScaling(EStateEffectProperty stateEffectProperty, float baseValue, float bonusValue, float scalingFactor = 0.1f, List<SStateEffectStackFactor> stateEffectStackFactors = default)
        {
            StateEffectProperty = stateEffectProperty;
            BaseValue = baseValue;
            BonusValue = bonusValue;
            ScalingFactor = scalingFactor;
            StateEffectStackFactors = stateEffectStackFactors != default ? stateEffectStackFactors : new List<SStateEffectStackFactor>() ;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StateEffectProperty);
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref BonusValue);
            serializer.SerializeValue(ref ScalingFactor);

            // -- PowerUps
            var length = StateEffectStackFactors != null ? StateEffectStackFactors.Count : 0;
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
                + BaseValue * Mathf.Pow(1 + ScalingFactor, level - 1)
                + GetStateEffectStackBonus(level, controller, targetController);
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
    }

    [CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
    public class CharacterData : CollectableData
    {
        #region Members

        public static readonly EStateEffectProperty[] INT_PROPERTIES = new EStateEffectProperty[] {
            EStateEffectProperty.MaxStacks,
            EStateEffectProperty.Shield,
            EStateEffectProperty.ResistanceFix,
            EStateEffectProperty.Damages,
            EStateEffectProperty.TickShield,
            EStateEffectProperty.BonusDamages,
            EStateEffectProperty.BonusTickDamages,
            EStateEffectProperty.BonusTickHeal,
            EStateEffectProperty.BonusTickShield,
            EStateEffectProperty.BonusBurnDamages,
            EStateEffectProperty.Hp,
            EStateEffectProperty.Stacks,
            EStateEffectProperty.EndDamages,
            EStateEffectProperty.EndHeal, 
        };

        // ===============================================================================================================
        // PUBLIC / SERIALIZABLE FIELDS
        [Header("Spells")]
        [SerializeField] protected string   m_AutoAttack;
        [SerializeField] protected string   m_SpecialAbility;
        [SerializeField] protected string   m_Ultimate;

        [Header("Stats")]
        public float            Size            = 1f;
        public float            BaseSpeed       = 1f;
        public int              BaseHealth      = 1000;
        public int              MaxEnergy       = 100;
        public int              BaseEnergy      = 10;
        public bool             IsStructure     = false;

        [Header("Bonus Stats")]
        [SerializeField] public float           HealthScaleFactor = 0.1f;
        public List<SCharacterStatScaling>      CharacterStatScaling;

        [Header("Trigger Effects")]
        [SerializeField]
        protected List<SRunePower> m_SpecialPowers = new();

        // ===============================================================================================================
        // DEPENDENT ACCESSORS
        protected override Type m_EnumType  => typeof(ECharacter);
        public ECharacter Character         => (ECharacter)Id;
        public ESpell AutoAttack            => ParseSpell(m_AutoAttack);
        public ESpell SpecialAbility        => ParseSpell( m_SpecialAbility);
        public ESpell Ultimate              => ParseSpell(m_Ultimate);
        public int MaxHealth                => (int)Math.Round(BaseHealth * Math.Pow(1 + HealthScaleFactor, m_Level - 1)) + (int)GetValue(EStateEffectProperty.Hp);
        public float Speed                  => BaseSpeed + GetValue(EStateEffectProperty.SpeedBonus);
        public List<SRunePower> SpecialPowers => m_SpecialPowers;

        #endregion


        #region Instantiation

        public GameObject InstantiateCharacterPreview(GameObject parent)
        {
            if (parent == null || parent.IsDestroyed())
                return null;

            var go = GameObject.Instantiate(AssetLoader.LoadCharacterPreview(Name), parent.transform);
            return go;
        }

        ESpell ParseSpell(string spellName)
        {
            if (spellName == "")
            {
                return ESpell.None;
            }

            if (!Enum.TryParse(spellName, out ESpell spell))
            {
                ErrorHandler.Error("Unable to parse " + spellName + " into spell");
                return ESpell.None;
            }

            return spell;
        }

        #endregion


        #region Cloning & Level

        public new CharacterData Clone(int level = 0, bool destroy = false)
        {
            return (CharacterData)base.Clone(level, destroy);
        }

        public override void SetLevel(int level)
        {
            base.SetLevel(level);
            for (int i = 0; i < m_SpecialPowers.Count; i++)
            {
                m_SpecialPowers[i].SetLevel(level);
            }
        }

        /// <summary>
        /// Add provided bonus base stats to the CharacterScaling values
        /// </summary>
        /// <param name="bonusStats"></param>
        public void AddBonusStats(List<SCharacterStatScaling> bonusStats)
        {
            foreach (var characterStatScaling in bonusStats)
            {
                int index = CharacterStatScaling.FindIndex(value => value.StateEffectProperty.Equals(characterStatScaling.StateEffectProperty));
                if (index < 0)
                {
                    CharacterStatScaling.Add(characterStatScaling.AsBonus(m_Level));
                    continue;
                }

                var current = CharacterStatScaling[index];
                current.BonusValue += characterStatScaling.AsBonus(m_Level).BonusValue;
                current.StateEffectStackFactors.AddRange(characterStatScaling.StateEffectStackFactors);
                CharacterStatScaling[index] = current;
            }
        }

        #endregion


        #region Scaling & Stats Accessors

        public float GetValue(EStateEffectProperty property, Controller caster = null, Controller targetController = null)
        {
            var characterStatScalingData = GetCharacterScalingData(property);
            if (! characterStatScalingData.HasValue)
                return 0.0f;

            return characterStatScalingData.Value.GetValue(Level, caster, targetController);
        }

        public int GetInt(EStateEffectProperty property, Controller caster = null, Controller targetController = null)
        {
            return (int)Math.Round(GetValue(property, caster, targetController));
        }

        SCharacterStatScaling? GetCharacterScalingData(EStateEffectProperty property)
        {
            foreach (SCharacterStatScaling data in CharacterStatScaling)
            {
                if (data.StateEffectProperty == property)
                    return data;
            }

            return null;
        }

        #endregion


        #region Checkers

        public static bool CheckIsInt(string property)
        {
            if (!Enum.TryParse(property, out EStateEffectProperty propertyValue))
                return false;

            return INT_PROPERTIES.Contains(propertyValue);
        }

        public static bool CheckIsPercentageValue(string property)
        {
            return property.EndsWith("Perc")
                || property == EStateEffectProperty.BonusLifeSteal.ToString()
                || property == EStateEffectProperty.BonusTickLifeSteal.ToString()
                || property == EStateEffectProperty.AttackSpeed.ToString()
                || property == EStateEffectProperty.CastSpeed.ToString()
                || property == EStateEffectProperty.LifeSteal.ToString()
                || property == EStateEffectProperty.SpeedBonus.ToString()
                ;
        }

        #endregion


        #region Infos

        public override Dictionary<string, object> GetInfos()
        {
            var infosDict = base.GetInfos();

            infosDict.Add("Health", MaxHealth);
            infosDict.Add("MovementSpeed", Speed);

            foreach (SCharacterStatScaling data in CharacterStatScaling)
            {
                // skip speed bonus (provided in Speed)
                if (data.StateEffectProperty == EStateEffectProperty.SpeedBonus)
                    continue;

                infosDict.Add(data.StateEffectProperty.ToString(), INT_PROPERTIES.Contains(data.StateEffectProperty) ? GetInt(data.StateEffectProperty) : GetValue(data.StateEffectProperty));
            }

            return infosDict;
        }

        #endregion
    }
}