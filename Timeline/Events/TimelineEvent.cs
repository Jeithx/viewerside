
using UnityEngine;

[System.Serializable]
public class TimelineEvent
{
    public float time;
    public string eventName;
    public bool triggered;

    public TimelineEvent(float time, string eventName)
    {
        this.time = time;
        this.eventName = eventName;
        this.triggered = false;
    }
    public void Reset()
    {
        triggered = false;
    }
}