using Data;
using Enums;
using System;
using System.Collections.Generic;
using Tools;

namespace Save.Data
{
    [Serializable]
    public class SGameEndedCloudData : SAnalyticsData
    {
        public EGameMode GameMode;
        public bool Win;
        public ECharacter Character;
        public int CharacterLevel;

        public SGameEndedCloudData(EGameMode gameMode, bool win, ECharacter character, int characterLevel) : base()
        {
            GameMode        = gameMode;
            Win             = win;
            Character       = character;
            CharacterLevel  = characterLevel;
        }

        public override object GetValue(EAnalyticsParam analyticsParam, bool throwError = true)
        {
            switch (analyticsParam)
            {
                case EAnalyticsParam.GameMode:
                    return GameMode;

                case EAnalyticsParam.Win:
                    return Win;

                case EAnalyticsParam.Character:
                    return Character;

                case EAnalyticsParam.CharacterLevel:
                    return CharacterLevel;

                default:
                    return base.GetValue(analyticsParam, throwError);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SGameEndedCloudData other)
            {
                return this.GameMode == other.GameMode
                       && this.Win == other.Win
                       && this.Character == other.Character
                       && this.CharacterLevel == other.CharacterLevel;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GameMode, Win, Character, CharacterLevel);
        }
    }
}