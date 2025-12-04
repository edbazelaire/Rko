using Enums;
using System.Collections.Generic;
using Tools;
using Unity.Services.Analytics;

public static class MAnalytics
{
    public static void Initialize()
    {
        // TODO : Consent UI
        bool userGaveConsent = true;

        if (userGaveConsent)
        {
            AnalyticsService.Instance.StartDataCollection();
        }
    }

    public static void SendEvent(Event myEvent)
    {
        // For demonstration purposes, log the event data to the console
        AnalyticsService.Instance.RecordEvent(myEvent);
    }

    /// <summary>
    /// Generic method to send any analytics event with event name and parameters
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    public static void SendEvent(EAnalytics eventName, Dictionary<string, object> eventData)
    {
        CustomEvent customEvent = new CustomEvent(eventName.ToString());
        foreach(var item in eventData)
        {
            customEvent.Add(item.Key, item.Value);
        }

        SendEvent(customEvent);
    }

    /// <summary>
    /// Method to log event data (for demonstration purposes)
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    private static void LogEvent(string eventName, Dictionary<string, object> eventData)
    {
        // Output the event name and data to the console
        ErrorHandler.Log(() => $"Event: {eventName}", ELogTag.Analytics);

        foreach (var entry in eventData)
        {
            ErrorHandler.Log(() => $"   {entry.Key}: {entry.Value}", ELogTag.Analytics);
        }
    }
}