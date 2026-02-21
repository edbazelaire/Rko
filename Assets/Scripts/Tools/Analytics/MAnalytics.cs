using Enums;
using System.Collections.Generic;
using Tools;
using Unity.Services.Analytics;

public static class MAnalytics
{
    static bool s_IsInitialized;

    public static void Initialize()
    {
        if (s_IsInitialized)
            return;

        // TODO : Consent UI
        bool userGaveConsent = true;

        if (userGaveConsent)
        {
            try
            {
                AnalyticsService.Instance.StartDataCollection();
                s_IsInitialized = true;
            }
            catch (System.Exception ex)
            {
                ErrorHandler.Error($"Unable to start analytics data collection: {ex.Message}");
            }
        }
    }

    public static void SendEvent(Event myEvent)
    {
        if (myEvent == null)
        {
            ErrorHandler.Warning("Trying to send a null analytics event");
            return;
        }

        if (!s_IsInitialized)
        {
            ErrorHandler.Warning("Analytics was not initialized, skipping event");
            return;
        }

        try
        {
            AnalyticsService.Instance.RecordEvent(myEvent);
        }
        catch (System.Exception ex)
        {
            ErrorHandler.Error($"Unable to record analytics event: {ex.Message}");
        }
    }

    /// <summary>
    /// Generic method to send any analytics event with event name and parameters
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    public static void SendEvent(EAnalytics eventName, Dictionary<string, object> eventData)
    {
        if (eventData == null)
        {
            ErrorHandler.Warning($"Trying to send analytics event {eventName} with null payload");
            return;
        }

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