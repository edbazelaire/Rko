using Enums;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data.GameManagement
{
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
        static void Load()
        {
            s_Instance = AssetLoader.Load<Settings>("PlayerSettings", AssetLoader.c_ManagementDataPath);

            //// reset player prefs
            //foreach (ESettings setting in Enum.GetValues(typeof(ESettings)))
            //{
            //    Set(setting, Get(setting));
            //}
        }

        #endregion


        #region Getters & Setters

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
    }
}