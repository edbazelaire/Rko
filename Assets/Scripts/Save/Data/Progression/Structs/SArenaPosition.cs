using Enums;
using System;


namespace Save.Data.Progression.Structs
{
    [Serializable]
    public struct SArenaPosition
    {
        public EArenaDifficulty ArenaDifficulty;
        public int ArenaLevel;
        public int ArenaStage;

        public SArenaPosition(EArenaDifficulty arenaDifficulty, int arenaLevel = -1, int arenaStage = -1)
        {
            ArenaDifficulty = arenaDifficulty;
            ArenaLevel      = arenaLevel;
            ArenaStage      = arenaStage;
        }

        // Overload the "==" operator
        public static bool operator ==(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty == rhs.ArenaDifficulty && lhs.ArenaLevel == rhs.ArenaLevel && lhs.ArenaStage == rhs.ArenaStage;
        }

        // Overload the "!=" operator
        public static bool operator !=(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty != rhs.ArenaDifficulty && lhs.ArenaLevel != rhs.ArenaLevel && lhs.ArenaStage != rhs.ArenaStage;
        }

        // Overload the ">=" operator
        public static bool operator >=(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty > rhs.ArenaDifficulty || lhs.ArenaLevel > rhs.ArenaLevel || (lhs.ArenaDifficulty == rhs.ArenaDifficulty && lhs.ArenaLevel == rhs.ArenaLevel && lhs.ArenaStage >= rhs.ArenaStage);
        }

        // Overload the "<=" operator
        public static bool operator <=(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty < rhs.ArenaDifficulty || lhs.ArenaLevel < rhs.ArenaLevel || (lhs.ArenaDifficulty == rhs.ArenaDifficulty && lhs.ArenaLevel == rhs.ArenaLevel && lhs.ArenaStage <= rhs.ArenaStage);
        }

        // Overload the ">" operator
        public static bool operator >(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty > rhs.ArenaDifficulty || lhs.ArenaLevel > rhs.ArenaLevel || (lhs.ArenaDifficulty == rhs.ArenaDifficulty && lhs.ArenaLevel == rhs.ArenaLevel && lhs.ArenaStage > rhs.ArenaStage);
        }

        // Overload the "<" operator
        public static bool operator <(SArenaPosition lhs, SArenaPosition rhs)
        {
            return lhs.ArenaDifficulty < rhs.ArenaDifficulty || lhs.ArenaLevel < rhs.ArenaLevel || (lhs.ArenaDifficulty == rhs.ArenaDifficulty && lhs.ArenaLevel == rhs.ArenaLevel && lhs.ArenaStage < rhs.ArenaStage);
        }

        // You also need to override Equals() and GetHashCode() when overloading == and !=
        public override bool Equals(object obj)
        {
            if (!(obj is SArenaPosition other))
            {
                return false;
            }
            return this == other;
        }

        public override int GetHashCode()
        {
            return ArenaDifficulty.GetHashCode() + ArenaLevel.GetHashCode() + ArenaStage.GetHashCode();
        }
    }
}