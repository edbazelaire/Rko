using Data.DataStructures.CharacterSubStructures;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
    public class CharacterData : CollectableData
    {
        #region Members

        public static readonly EStateEffectProperty[] INT_PROPERTIES = new EStateEffectProperty[] {
            EStateEffectProperty.MaxStacks,
            EStateEffectProperty.Shield,
            EStateEffectProperty.ResistanceFix,
            EStateEffectProperty.Damage,
            EStateEffectProperty.Heal,
            EStateEffectProperty.HealReduction,
            EStateEffectProperty.BonusDamage,
            EStateEffectProperty.BonusExecutionDamage,
            EStateEffectProperty.BonusTickDamage,
            EStateEffectProperty.BonusTickHeal,
            EStateEffectProperty.BonusTickShield,
            EStateEffectProperty.BonusBurnDamage,
            EStateEffectProperty.Hp,
            EStateEffectProperty.Stacks,
            EStateEffectProperty.EndDamage,
            EStateEffectProperty.EndHeal,
            EStateEffectProperty.TickDamage,
            EStateEffectProperty.TickHeal,
            EStateEffectProperty.TickShield,
            EStateEffectProperty.TickEnergy,
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
        public int MaxHealth                => (int)Math.Round(BaseHealth * Math.Pow(1 + HealthScaleFactor, m_Level - 1)) + (int)GetValue(EStateEffectProperty.Hp, "");
        public float Speed                  => BaseSpeed + GetValue(EStateEffectProperty.SpeedBonus, "");
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

        public void AddBonusStat(EStateEffectProperty property, float value, List<string> specialConditions)
        {
            AddBonusStats(new List<SCharacterStatScaling>() { new SCharacterStatScaling(property, 0f, value, 0f, specialConditions) });
        }

        /// <summary>
        /// Add provided bonus base stats to the CharacterScaling values
        /// </summary>
        /// <param name="bonusStats"></param>
        public void AddBonusStats(List<SCharacterStatScaling> bonusStats)
        {
            foreach (var characterStatScaling in bonusStats)
            {
                int index = CharacterStatScaling.FindIndex(value => value.StateEffectProperty.Equals(characterStatScaling.StateEffectProperty) && value.SpecialConditions == characterStatScaling.SpecialConditions);
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

        public float GetValue(EStateEffectProperty property, string specialCondition, Controller caster = null, Controller targetController = null)
        {
            float value = 0f;
            var characterStatScalingData = GetCharacterScalingData(property, specialCondition);
            foreach (SCharacterStatScaling characterStatScaling in characterStatScalingData)
            {
                value += characterStatScaling.GetValue(m_Level, caster, targetController);
            }

            return value;
      
        }

        public int GetInt(EStateEffectProperty property, string specialCondition, Controller caster = null, Controller targetController = null)
        {
            return (int)Math.Round(GetValue(property, specialCondition, caster, targetController));
        }

        List<SCharacterStatScaling> GetCharacterScalingData(EStateEffectProperty property, string specialCondition)
        {
            return CharacterStatScaling.Where(t => t.StateEffectProperty == property && t.HasSpecialCondition(specialCondition)).ToList();
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
                // STATE EFFECT properties
                || property == EStateEffectProperty.BonusLifeSteal.ToString()
                || property == EStateEffectProperty.BonusTickLifeSteal.ToString()
                || property == EStateEffectProperty.AttackSpeed.ToString()
                || property == EStateEffectProperty.CastSpeed.ToString()
                || property == EStateEffectProperty.LifeSteal.ToString()
                || property == EStateEffectProperty.SpeedBonus.ToString()

                // SPELL properties
                || property == ESpellProperty.GrowSizeFactor.ToString()
                ;
        }

        #endregion


        #region Infos

        public override Dictionary<string, object> GetInfo()
        {
            var infosDict = base.GetInfo();

            infosDict.Add("Health", MaxHealth);
            infosDict.Add("MovementSpeed", Speed);

            foreach (SCharacterStatScaling data in CharacterStatScaling)
            {
                // skip speed bonus (provided in Speed)
                if (data.StateEffectProperty == EStateEffectProperty.SpeedBonus)
                    continue;

                // Normal stat
                if (data.SpecialConditions.Count == 0)
                    infosDict.Add(data.StateEffectProperty.ToString(), INT_PROPERTIES.Contains(data.StateEffectProperty) ? Math.Round(data.GetValue(m_Level)) : data.GetValue(m_Level));
                
                // Special Conditions : add separately
                else
                {
                    foreach (var specialCondition in data.SpecialConditions)
                    {
                        infosDict.Add(TextHandler.FormatSpecialPropertyName(data.StateEffectProperty.ToString(), specialCondition), INT_PROPERTIES.Contains(data.StateEffectProperty) ? Math.Round(data.GetValue(m_Level)) : data.GetValue(m_Level));
                    }
                }
            }

            return infosDict;
        }

        #endregion
    }
}