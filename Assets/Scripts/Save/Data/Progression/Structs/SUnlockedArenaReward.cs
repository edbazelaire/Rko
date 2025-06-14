using Enums;
using System;


namespace Save.Data.Progression.Structs
{
    [Serializable]
    public struct SUnlockedArenaReward
    {
        public SArenaDifficulty ArenaDifficulty;
        public int ArenaLevel;

        public SUnlockedArenaReward(SArenaDifficulty arenaDifficulty, int arenaLevel = -1)
        {
            ArenaDifficulty = arenaDifficulty != null ? arenaDifficulty : new SArenaDifficulty(EArenaDifficulty.Easy, 0);
            ArenaLevel = arenaLevel;
        }
    }
}