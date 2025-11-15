using Enums;
using System.Collections.Generic;

namespace Analytics.Events
{
    public class ArenaGameEndedEvent : GameEndedEvent
    {
        public ArenaGameEndedEvent(bool win, ECharacter character, int playerLevel, ERune[] runes, List<ESpell> spells, List<int> spellLevels, EArenaType arenaType, EArenaDifficulty arenaDifficulty, int level, int stage, int extraDifficulty, List<EArenaMod> arenaMods) : base(EGameMode.Arena, win, character, playerLevel, runes, spells, spellLevels, EAnalytics.ArenaGameEnded) 
        {
            // Additional parameters specific to ArenaGameEndedEvent
            SetParameter("ArenaType", arenaType.ToString());
            SetParameter("ArenaDifficulty", arenaDifficulty.ToString());
            SetParameter("Level", level);
            SetParameter("Stage", stage);
            SetParameter("ExtraDifficulty", extraDifficulty);
            SetParameter("ArenaMods", string.Join(", ", arenaMods));
        }
    }
}

