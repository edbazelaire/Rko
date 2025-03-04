using Enums;
using Save;
using Save.Data;
using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class InGameEvent : Event
    {
        protected static EAnalytics EventType => EAnalytics.InGame;

        // Constructor for GameEnded event
        public InGameEvent(EGameMode gameMode, string gameId, ECharacter character, string spell, string hitType, int qty) : base(EventType.ToString())
        {
            // Set event parameters using SetParameter method
            SetParameter(EAnalyticsParam.GameMode.ToString(),   gameMode.ToString());
            SetParameter(EAnalyticsParam.GameId.ToString(),     gameId.ToString());
            SetParameter(EAnalyticsParam.Character.ToString(),  character.ToString());
            SetParameter(EAnalyticsParam.Spell.ToString(),      spell.ToString());
            SetParameter(EAnalyticsParam.HitType .ToString(),   hitType.ToString());
            SetParameter(EAnalyticsParam.Qty.ToString(),        qty);

            StatCloudData.AddAnalytics(EventType, new SInGameEventCloudData(gameMode, character, spell, hitType, qty));
        }
    }
}

