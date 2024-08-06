using Enums;
using System.Collections.Generic;
using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class RankedGameEndedEvent : GameEndedEvent
    {
        // Constructor for SoloGameEndedEvent
        public RankedGameEndedEvent(bool win, ECharacter character, int characterLevel, ERune rune, List<ESpell> spells, List<int> spellLevels, string gameId, ELeague league, int level, int stage) : base(EGameMode.Ranked, win, character, characterLevel, rune, spells, spellLevels, EAnalytics.RankedGameEnded)
        {
            // Additional parameters specific to SoloGameEndedEvent
            SetParameter("GameId",          gameId);
            SetParameter("League",          league.ToString());
            SetParameter("Level",           level);
            SetParameter("Stage",           stage);
        }
    }
}

