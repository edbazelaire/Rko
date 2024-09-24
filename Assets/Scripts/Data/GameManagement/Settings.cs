using Game;
using System;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

namespace Data.GameManagement
{
    public enum ESettings
    {
        CharacterSizeFactor,
        CharacterSpeedFactor,
        SpellSizeFactor,
        SpellSpeedFactor,
        AutoAttackSpeedFactor,
        CastSpeedFactor,
        SpellFixedDistance,
    }

    [CreateAssetMenu(fileName = "Settings", menuName = "Game/Management/Settings")]
    public class Settings : ScriptableObject
    {
        #region Members

        public const float SPELL_HIGHT_POS_Y    = 4f;
        public const float SPELL_DIAGONAL_POS_Y = 4f;

        // GAME Speed & Size
        [Header("Game Speed & Size")]
        [SerializeField] float m_CharacterSizeFactor;
        [SerializeField] float m_CharacterSpeedFactor;
        [SerializeField] float m_SpellSizeFactor;
        [SerializeField] float m_SpellSpeedFactor;
        [SerializeField] float m_AutoAttackSpeedFactor;
        [SerializeField] float m_CastSpeedFactor;

        [Header("Distances")]
        [SerializeField] float m_SpellFixedDistance = 7f;

        public static float CharacterSizeFactor     { get => Get(ESettings.CharacterSizeFactor);    }
        public static float CharacterSpeedFactor    { get => Get(ESettings.CharacterSpeedFactor);   }
        public static float SpellSizeFactor         { get => Get(ESettings.SpellSizeFactor);        }
        public static float SpellSpeedFactor        { get => Get(ESettings.SpellSpeedFactor);       }
        public static float AutoAttackSpeedFactor   { get => Get(ESettings.AutoAttackSpeedFactor);  }
        public static float CastSpeedFactor         { get => Get(ESettings.CastSpeedFactor);        }
        public static float SpellFixedDistance      { get => Get(ESettings.SpellFixedDistance);     }

        #endregion


        #region Instance

        static Settings s_Instance;

        public static Settings Instance
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

        public static void Reload()
        {
            // reset player prefs
            foreach (string setting in Enum.GetNames(typeof(ESettings)))
            {
                PlayerPrefs.DeleteKey(setting);
            }

            Load();
        }

        static void Load()
        {
            s_Instance = AssetLoader.Load<Settings>("Settings", AssetLoader.c_ManagementDataPath);

            // reset player prefs
            foreach (ESettings setting in Enum.GetValues(typeof(ESettings)))
            {
                Set(setting, Get(setting));
            }
        }

        #endregion


        #region Getters & Setters

        public static float Get(ESettings setting, bool defaultOnly = false)
        {
            float defaultValue;
            switch (setting)
            {
                case ESettings.CharacterSizeFactor:
                    defaultValue = Instance.m_CharacterSizeFactor;
                    break;
                case ESettings.CharacterSpeedFactor:
                    defaultValue = Instance.m_CharacterSpeedFactor;
                    break;
                case ESettings.SpellSizeFactor:
                    defaultValue = Instance.m_SpellSizeFactor;
                    break;
                case ESettings.AutoAttackSpeedFactor:
                    defaultValue = Instance.m_AutoAttackSpeedFactor;
                    break;
                case ESettings.SpellSpeedFactor:
                    defaultValue = Instance.m_SpellSpeedFactor;
                    break;
                case ESettings.CastSpeedFactor:
                    defaultValue = Instance.m_CastSpeedFactor;
                    break;
                case ESettings.SpellFixedDistance:
                    defaultValue = Instance.m_SpellFixedDistance;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled setting : " + setting);
                    defaultValue = 1f;
                    break;
            }

            return PlayerPrefs.GetFloat(setting.ToString(), defaultValue);
        }

        public static void Set(ESettings setting, float value)
        {
            if (value <= 0)
            {
                ErrorHandler.Error("Trying to set " + setting + " with a value <= 0 : " + value);
                return;
            }

            PlayerPrefs.SetFloat(setting.ToString(), value);
        }

        #endregion
    }
}