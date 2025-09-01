using UnityEngine;
using TMPro;

public class TimelineEventMarker : MonoBehaviour
{
    public TimelineEvent timelineEvent;
    public TextMeshProUGUI eventNameText;

    public void Initialize(TimelineEvent newEvent)
    {
        timelineEvent = newEvent;
        if (eventNameText != null)
        {
            eventNameText.text = timelineEvent.eventName;
        }
    }

    public void TriggerEvent()
    {
        if (!timelineEvent.triggered)
        {
            Debug.Log($"Event: '{timelineEvent.eventName}' tetiklendi, zaman: {timelineEvent.time:F2}");
            timelineEvent.triggered = true;
        }
    }
}