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
        public GameEndedEvent(EGameMode gameMode, bool win, ECharacter character, int playerLevel, ERune rune, List<ESpell> spells, List<int> spellLevels, EAnalytics eventType = EAnalytics.GameEnded) : base(eventType.ToString())
        {
            // Set event parameters using SetParameter method
            SetParameter(EAnalyticsParam.GameMode.ToString(),       gameMode.ToString());
            SetParameter(EAnalyticsParam.Win.ToString(),            win);
            SetParameter(EAnalyticsParam.Character.ToString(),      character.ToString());
            SetParameter(EAnalyticsParam.Rune.ToString(),           rune.ToString());
            SetParameter(EAnalyticsParam.Spells.ToString(),         String.Join(",", spells.ToArray()));
            SetParameter(EAnalyticsParam.SpellLevels.ToString(),    String.Join(",", spellLevels.ToArray()));

            StatCloudData.AddAnalytics(EAnalytics.GameEnded, new SGameEndedCloudData(gameMode, win, character));
        }
    }
}

