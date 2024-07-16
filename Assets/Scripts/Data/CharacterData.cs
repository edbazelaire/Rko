using Enums;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEngine.Rendering.DebugUI;

namespace Data
{
    [Serializable]
    public struct SCharacterStatScaling : INetworkSerializable
    {
        public EStateEffectProperty StateEffectProperty;
        public float BaseValue;
        public float ScalingFactor;

        public SCharacterStatScaling(EStateEffectProperty stateEffectProperty, float baseValue, float scalingFactor = 0.1f)
        {
            StateEffectProperty = stateEffectProperty;
            BaseValue = baseValue;
            ScalingFactor = scalingFactor;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StateEffectProperty);
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref ScalingFactor);
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
            EStateEffectProperty.Hp,
            EStateEffectProperty.Stacks,
            EStateEffectProperty.EndDamages,
            EStateEffectProperty.EndHeal, 
        };

        // ===============================================================================================================
        // PUBLIC / SERIALIZABLE FIELDS
        [Header("Spells")]
        public ESpell           AutoAttack;
        public ESpell           SpecialAbility;
        public ESpell           Ultimate;

        [Header("Stats")]
        public float            Size        = 1f;
        public float            BaseSpeed   = 1f;
        public int              BaseHealth  = 1000;
        public int              MaxEnergy   = 100;

        [Header("Bonus Stats")]
        [SerializeField] public float           HealthScaleFactor = 0.1f;
        public List<SCharacterStatScaling>      CharacterStatScaling;

        // ===============================================================================================================
        // DEPENDENT ACCESSORS
        protected override Type m_EnumType => typeof(ECharacter);
        public ECharacter Character => (ECharacter)Id;
        public int MaxHealth => (int)Math.Round(BaseHealth * Math.Pow(1 + HealthScaleFactor, m_Level - 1)) + (int)GetValue(EStateEffectProperty.Hp);
        public float Speed => BaseSpeed + GetValue(EStateEffectProperty.SpeedBonus);

        #endregion


        #region Instantiation

        public GameObject InstantiateCharacterPreview(GameObject parent)
        {
            var go = GameObject.Instantiate(AssetLoader.LoadCharacterPreview(Character), parent.transform);
            return go;
        }

        #endregion


        #region Cloning & Level

        public new CharacterData Clone(int level = 0)
        {
            return (CharacterData)base.Clone(level);
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
                    CharacterStatScaling.Add(characterStatScaling);
                    return;
                }

                var current = CharacterStatScaling[index];
                current.BaseValue += characterStatScaling.BaseValue;
                current.ScalingFactor += characterStatScaling.ScalingFactor;

                CharacterStatScaling[index] = current;
            }
        }

        #endregion


        #region Scaling & Stats Accessors

        public float GetValue(EStateEffectProperty property)
        {
            var characterStatScalingData = GetCharacterScalingData(property);
            if (!characterStatScalingData.HasValue)
                return 0.0f;

            return Mathf.Pow(1f + characterStatScalingData.Value.ScalingFactor, m_Level - 1) * characterStatScalingData.Value.BaseValue;
        }

        public int GetInt(EStateEffectProperty property)
        {
            return (int)Math.Round(GetValue(property));
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