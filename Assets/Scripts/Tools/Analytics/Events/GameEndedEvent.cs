using Enums;
using Save;
using Save.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class GameEndedEvent : Event
    {
        // Constructor for GameEnded event
        public GameEndedEvent(EGameMode gameMode, bool win, ECharacter character, int playerLevel, ERune[] runes, List<ESpell> spells, List<int> spellLevels, EAnalytics eventType = EAnalytics.GameEnded) : base(eventType.ToString())
        {
            string runesPayload = runes == null ? "" : string.Join(",", runes.Select(r => r.ToString()));
            string spellsPayload = spells == null ? "" : string.Join(",", spells.Select(s => s.ToString()));
            string spellLevelsPayload = spellLevels == null ? "" : string.Join(",", spellLevels);

            // Set event parameters using SetParameter method
            SetParameter(EAnalyticsParam.GameMode.ToString(),       gameMode.ToString());
            SetParameter(EAnalyticsParam.Win.ToString(),            win);
            SetParameter(EAnalyticsParam.Character.ToString(),      character.ToString());
            SetParameter(EAnalyticsParam.CharacterLevel.ToString(), playerLevel);
            SetParameter(EAnalyticsParam.Rune.ToString(),           runesPayload);
            SetParameter(EAnalyticsParam.Spells.ToString(),         spellsPayload);
            SetParameter(EAnalyticsParam.SpellLevels.ToString(),    spellLevelsPayload);

            StatCloudData.AddAnalytics(EAnalytics.GameEnded, new SGameEndedCloudData(gameMode, win, character, playerLevel));
        }
    }
}

