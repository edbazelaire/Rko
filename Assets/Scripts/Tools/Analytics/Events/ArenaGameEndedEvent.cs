using Enums;
using System.Collections.Generic;
using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class ArenaGameEndedEvent : GameEndedEvent
    {
        public ArenaGameEndedEvent(bool win, ECharacter character, int playerLevel, ERune rune, List<ESpell> spells, List<int> spellLevels, EArenaType arenaType, EArenaDifficulty arenaDifficulty, int level, int stage) : base(EGameMode.Arena, win, character, playerLevel, rune, spells, spellLevels, EAnalytics.ArenaGameEnded) 
        {
            // Additional parameters specific to ArenaGameEndedEvent
            SetParameter("ArenaType", arenaType.ToString());
            SetParameter("ArenaDifficulty", arenaDifficulty.ToString());
            SetParameter("Level", level);
            SetParameter("Stage", stage);
        }
    }
}

