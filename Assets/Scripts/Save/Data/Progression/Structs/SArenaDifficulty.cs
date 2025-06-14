using Data.GameManagement;
using Data;
using Enums;
using System.Linq;
using System;
using Tools;


namespace Save.Data.Progression.Structs
{
    [Serializable]
    public struct SArenaDifficulty
    {
        public EArenaDifficulty Difficulty;
        public int Level;

        public SArenaDifficulty(EArenaDifficulty difficulty = 0, int level = 1)
        {
            Difficulty = difficulty;
            Level = level;
        }

        public override string ToString() => Difficulty + " " + new string('+', Level);

        public SArenaDifficulty FromString(string str)
        {
            var splits = str.Split(" ");
            if (splits.Length != 2)
            {
                ErrorHandler.Error("Bad SArenaDifficulty string : " + str);
                return this;
            }

            if (!Enum.TryParse(splits[0], out Difficulty))
            {
                ErrorHandler.Error("Bad SArenaDifficulty string - unable to parse difficulty : " + str);
                return this;
            }

            Level = splits[1].Count(f => f == '+'); ;

            return this;
        }

        // Overload the "==" operator
        public static bool operator ==(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty == rhs.Difficulty && lhs.Level == rhs.Level;
        }

        // Overload the "!=" operator
        public static bool operator !=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty != rhs.Difficulty && lhs.Level != rhs.Level;
        }

        // Overload the ">=" operator
        public static bool operator >=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty > rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level >= rhs.Level);
        }

        // Overload the "<=" operator
        public static bool operator <=(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty < rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level <= rhs.Level);
        }

        // Overload the ">" operator
        public static bool operator >(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty > rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level > rhs.Level);
        }

        // Overload the "<" operator
        public static bool operator <(SArenaDifficulty lhs, SArenaDifficulty rhs)
        {
            return lhs.Difficulty < rhs.Difficulty || (lhs.Difficulty == rhs.Difficulty && lhs.Level < rhs.Level);
        }

        // You also need to override Equals() and GetHashCode() when overloading == and !=
        public override bool Equals(object obj)
        {
            if (!(obj is SArenaDifficulty))
            {
                return false;
            }
            var other = (SArenaDifficulty)obj;
            return this.Difficulty == other.Difficulty && this.Level == other.Level;
        }

        public override int GetHashCode()
        {
            return Difficulty.GetHashCode() + Level.GetHashCode();
        }
    }
}