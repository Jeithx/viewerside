using System.Collections.Generic;
using UnityEngine;
using static TimelineXMLSerializer;

public class PopupEventManager : MonoBehaviour
{
    private Timer timer;
    private TimelineXMLSerializer serializer;

    private List<PopupEventData> events = new List<PopupEventData>();

    private void Awake()
    {
        timer = GetComponent<Timer>();
        serializer = GetComponent<TimelineXMLSerializer>();
    }

    public void LoadEventsFromSerializer()
    {
        events.Clear();
        foreach (var e in serializer.LoadedPopupEvents)
        {
            events.Add(new PopupEventData
            {
                index = e.index,
                text = e.text,
                triggerTime = e.triggerTime
            });
        }
    }

    private void Update()
    {
        if (timer == null || events.Count == 0) return;

        float t = timer.getCurrentTime();

        foreach (var ev in events)
        {
            if (!evFired.Contains(ev) && t >= ev.triggerTime)
            {
                Debug.Log($"[PopupEvent #{ev.index}] {ev.text} @t={ev.triggerTime:F3}s");
                evFired.Add(ev);
            }
            if (evFired.Contains(ev) && t < ev.triggerTime)
            {
                evFired.Remove(ev);
            }
        }
    }

    private HashSet<PopupEventData> evFired = new HashSet<PopupEventData>();
}
