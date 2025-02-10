using Enums;
using System;
using UnityEngine;

namespace Tools
{
    public enum EPlayerPref
    {
        PlayerName,
        Region,
        GameMode,
        ArenaType,
        CurrentGameId,

        WarningMessageAccepted,

        TrainingCharacter,
        TrainingSpell,
        TrainingRune,
        TrainingDecisionRefresh,
        TrainingRandomness,
    }

    public enum EDebugOption
    {
        Console,
        Monitor,
        ErrorHandler,
        DebugMode,
    }

    public static class PlayerPrefsHandler
    {
        #region Members

        public static Action<EGameMode> GameModeChangedEvent;
        public static Action<EArenaType> ArenaTypeChangedEvent;

        public const EArenaType DEFAULT_ARENA_TYPE = EArenaType.FrostArena;

        #endregion


        #region Init & End

        public static void Initialize()
        {
            if (PlayerPrefs.GetString(EPlayerPref.PlayerName.ToString()) == "")
                PlayerPrefs.SetString(EPlayerPref.PlayerName.ToString(), "SheepRapist");

            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.GameMode.ToString()), out EGameMode gameMode))
                PlayerPrefs.SetString(EPlayerPref.GameMode.ToString(), EGameMode.Arena.ToString());

            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.ArenaType.ToString()), out EArenaType arena))
                SetArenaType(EArenaType.FrostArena);

            if (PlayerPrefs.GetInt(EPlayerPref.WarningMessageAccepted.ToString()) == 0)
                SetWarningAccepted(false);
        }


        #endregion


        #region General Getters & Setters

        public static void SetString(EPlayerPref pref, string value, int index = -1)
        {
            string name = pref.ToString();
            if (index >= 0)
            {
                name += "_" + index;
            }

            ErrorHandler.Warning("Saved " + name + " with value : " + value);

            PlayerPrefs.SetString(name, value);
            PlayerPrefs.Save();
        }

        public static TEnum GetString<TEnum>(EPlayerPref name, int index = -1)
        {
            return GetString<TEnum>(name.ToString(), index);
        }

        public static TEnum GetString<TEnum>(string name, int index = -1)
        {
            if (index >= 0)
            {
                name += "_" + index;
            }

            string value = PlayerPrefs.GetString(name, GetDefaultValue<TEnum>().ToString());
            if (!Enum.TryParse(typeof(TEnum), value, out object result))
            {
                ErrorHandler.Error("Unable to parse " + value + " into " + typeof(TEnum));
                return (TEnum)(object)GetDefaultValue<TEnum>();
            }

            return (TEnum)result;
        }

        public static Enum GetDefaultValue<TEnum>()
        {
            var enumType = typeof(TEnum);

            if (enumType == typeof(ECharacter))
                return ECharacter.Alexander;

            if (enumType == typeof(ESpell))
                return ESpell.Heal;

            if (enumType == typeof(ERune))
                return ERune.None;

            ErrorHandler.Error("No default value provided for " + enumType);
            return default;
        }

        #endregion


        #region Getters & Setters

        public static EGameMode GetGameMode()
        {
            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.GameMode.ToString()), out EGameMode gameMode))
            {
                ErrorHandler.Error("Unable to parse game mode : " + PlayerPrefs.GetString(EPlayerPref.GameMode.ToString()));
                gameMode = EGameMode.Arena;
                SetGameMode(EGameMode.Arena);
            }

            return gameMode;
        }

        public static void SetGameMode(EGameMode gameMode)
        {
            PlayerPrefs.SetString(EPlayerPref.GameMode.ToString(), gameMode.ToString());
            PlayerPrefs.Save();

            GameModeChangedEvent?.Invoke(gameMode);
        }

        public static EArenaType GetArenaType()
        {
            if (!Enum.TryParse(PlayerPrefs.GetString(EPlayerPref.ArenaType.ToString()), out EArenaType arenaType) || arenaType == EArenaType.None)
            {
                ErrorHandler.Error("Unable to parse solo arena : " + PlayerPrefs.GetString(EPlayerPref.ArenaType.ToString()));
                arenaType = DEFAULT_ARENA_TYPE;
                SetArenaType(arenaType);
            }

            return arenaType;
        }

        public static void SetArenaType(EArenaType arenaType)
        {
            PlayerPrefs.SetString(EPlayerPref.ArenaType.ToString(), arenaType.ToString());
            PlayerPrefs.Save();

            ArenaTypeChangedEvent?.Invoke(arenaType);
        }

        public static void SetVolume(EVolumeOption volume, float value)
        {
            if (value < 0 || value > 1)
            {
                ErrorHandler.Error("Bad volume provided : " + value);
                value = Mathf.Clamp(value, 0, 1);
            }

            PlayerPrefs.SetFloat(volume.ToString(), value);
            PlayerPrefs.Save();
        }

        public static float GetVolume(EVolumeOption volume)
        {
            return PlayerPrefs.GetFloat(volume.ToString(), 1f);
        }

        public static void SetMuted(EVolumeOption volume, int muted)
        {
            PlayerPrefs.SetInt(volume.ToString() + "_Muted", muted);
            PlayerPrefs.Save();
        }

        public static bool GetMuted(EVolumeOption volume)
        {
            return PlayerPrefs.GetInt(volume.ToString() + "_Muted", 0) == 1;
        }

        public static void SetWarningAccepted(bool accepted)
        {
            PlayerPrefs.SetInt(EPlayerPref.WarningMessageAccepted.ToString(), accepted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool GetWarningAccepted()
        {
            return PlayerPrefs.GetInt(EPlayerPref.WarningMessageAccepted.ToString(), 0) > 0;
        }

        public static void SetDebug(EDebugOption option, bool activate)
        {
            PlayerPrefs.SetInt(option.ToString(), activate ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool GetDebug(EDebugOption option)
        {
            return PlayerPrefs.GetInt(option.ToString(), 0) == 1;
        }

        public static ESpell[] GetTrainingSpells()
        {
            return new ESpell[] {
                GetString<ESpell>(EPlayerPref.TrainingSpell, 0),
                GetString<ESpell>(EPlayerPref.TrainingSpell, 1),
                GetString<ESpell>(EPlayerPref.TrainingSpell, 2),
                GetString<ESpell>(EPlayerPref.TrainingSpell, 3),
            };
        }

        public static ERune[] GetTrainingRunes()
        {
            return new ERune[] {
                GetString<ERune>(EPlayerPref.TrainingRune, 0),
                GetString<ERune>(EPlayerPref.TrainingRune, 1),
                GetString<ERune>(EPlayerPref.TrainingRune, 2),
            };
        }

        #endregion
    }
}