using Enums;
using Save;
using Save.Data;
using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class PlayerDataEvent : Event
    {
        protected static EAnalytics EventType => EAnalytics.PlayerData;

        // Constructor for GameEnded event
        public PlayerDataEvent(string pseudo, string token, string region) : base(EventType.ToString())
        {
            SetParameter("Pseudo",  pseudo);
            SetParameter("Token",   token);
            SetParameter("Region",  region);
        }
    }
}

