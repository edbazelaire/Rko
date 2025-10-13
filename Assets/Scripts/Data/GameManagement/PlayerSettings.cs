using Enums;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Data.GameManagement
{
    [Serializable]
    public struct SDamageTypeColor
    {
        public EHitCategory DamageType;
        public Color Color;
    }

    [Serializable]
    public struct SSpecialValueColor
    {
        public ESpecialValue SpecialValue;
        public Color Color;
    }

    [Serializable]
    public struct SHitTypeColor
    {
        public EHitType                 HitType;
        public List<SDamageTypeColor>   Colors;
    }

    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Game/Management/PlayerSettings")]
    public class PlayerSettings : ScriptableObject
    {
        #region Members

        // GAME Speed & Size
        [Header("Game Controls")]
        static Dictionary<ESpellSlot, KeyCode> m_SpellKeys = new()
        {
            { ESpellSlot.AutoAttack,        KeyCode.None    },
            { ESpellSlot.SpecialAbility,    KeyCode.Z       },
            { ESpellSlot.Ultimate,          KeyCode.R       },
            { ESpellSlot.Spell1,            KeyCode.Alpha1  },
            { ESpellSlot.Spell2,            KeyCode.Alpha2  },
            { ESpellSlot.Spell3,            KeyCode.Alpha3  },
            { ESpellSlot.Spell4,            KeyCode.Alpha4  },
        };

        [Header("UI")]
        [SerializeField] List<SHitTypeColor> m_HitTypeColor;
        [SerializeField] List<SSpecialValueColor> m_SpecialValueColor;

        // =======================================================================================
        // Public properties
        public static List<SHitTypeColor> HitTypeColor => Instance.m_HitTypeColor;
        public static List<SSpecialValueColor> SpecialValueColor => Instance.m_SpecialValueColor;

        #endregion


        #region Instance

        static PlayerSettings s_Instance;

        public static PlayerSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    Load();
                }

                return s_Instance;
            }
        }
        static void Load()
        {
            s_Instance = AssetLoader.Load<PlayerSettings>("PlayerSettings", AssetLoader.c_ManagementDataPath);

            //// reset player prefs
            //foreach (ESettings setting in Enum.GetValues(typeof(ESettings)))
            //{
            //    Set(setting, Get(setting));
            //}
        }

        #endregion


        #region Keys

        public static KeyCode GetKeyAtIndex(int index)
        {
            return GetKey((ESpellSlot)index);
        }

        public static KeyCode GetKey(ESpellSlot slot)
        {
            if (KeyCode.TryParse(PlayerPrefs.GetString(slot + "Key"), out KeyCode key))
                return key;

            return m_SpellKeys[slot];

        }

        public static void SetKey(ESpellSlot slot, KeyCode value)
        {
            PlayerPrefs.SetString(slot+"Key", value.ToString());
        }

        #endregion


        #region HitDisplay

        public static bool IsDisplayed(EHitType hitType, EHitCategory damageType)
        {
            return true;
        }

        public static Color GetHitTypeColor(EHitType hitType, EHitCategory damageType)
        {
            var hitTypeColor = HitTypeColor.Where((sHitTypeColor) => sHitTypeColor.HitType == hitType).FirstOrDefault();
            return hitTypeColor.Colors.Where((damageTypeColor) => damageTypeColor.DamageType == damageType).FirstOrDefault().Color;
        }

        public static Color GetSpecialValueColor(ESpecialValue specialValue)
        {
            return SpecialValueColor.Where((sHitTypeColor) => sHitTypeColor.SpecialValue == specialValue).FirstOrDefault().Color;
        }

        #endregion
    }
}