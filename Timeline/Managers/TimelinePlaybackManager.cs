using System;
using System.Collections.Generic;
using UnityEngine;

public class TimelinePlaybackManager : MonoBehaviour
{
    private Timer timer;
    private bool isPlaying = false;

    public List<VideoController> videoControllers = new List<VideoController>();
    public List<ImageController> imageControllers = new List<ImageController>();
    public List<AudioController> audioControllers = new List<AudioController>();
    public List<ModelController> modelControllers = new List<ModelController>();

    public event Action<float> OnTimeUpdated;
    public event Action<bool> OnPlayStateChanged;

    private List<TimelineEvent> timelineEvents = new List<TimelineEvent>();

    public bool IsPlaying => isPlaying;

    public void Initialize(GameObject timerObject)
    {
        if (timerObject)
            timer = timerObject.GetComponent<Timer>();
    }

    public void Play()
    {
        isPlaying = true;
        timer?.startTimer();

        float now = GetTime();

        foreach (var v in videoControllers)
            if (v != null) v.ScrubTo(now, shouldPlay: true);
        foreach (var i in imageControllers)
            if (i != null) i.ScrubTo(now, shouldPlay: true);
        foreach (var a in audioControllers)
            if (a != null) a.ScrubTo(now, shouldPlay: true);
        foreach (var m in modelControllers)
            if (m != null) m.ScrubTo(now, shouldPlay: true);

        OnPlayStateChanged?.Invoke(true);

        foreach (var ev in timelineEvents)
        {
            ev.Reset();
        }
    }

    public void Stop()
    {
        isPlaying = false;
        timer?.stopTimer();

        float now = GetTime();

        foreach (var v in videoControllers)
            if (v != null) v.ScrubTo(now, shouldPlay: false);
        foreach (var i in imageControllers)
            if (i != null) i.ScrubTo(now, shouldPlay: false);
        foreach (var a in audioControllers)
            if (a != null) a.ScrubTo(now, shouldPlay: false);
        foreach (var m in modelControllers)
            if (m != null) m.ScrubTo(now, shouldPlay: false);

        OnPlayStateChanged?.Invoke(false);

        foreach (var ev in timelineEvents)
        {
            ev.triggered = false;
        }
    }

    public void SetTime(float time)
    {
        time = Mathf.Max(0f, time);
        timer?.SetCurrentTime(time);

        ResetEventsIfNeeded(time);


        foreach (var v in videoControllers)
            if (v != null) v.ScrubTo(time, isPlaying);
        foreach (var i in imageControllers)
            if (i != null) i.ScrubTo(time, isPlaying);
        foreach (var a in audioControllers)
            if (a != null) a.ScrubTo(time, isPlaying);
        foreach (var m in modelControllers)
            if (m != null) m.ScrubTo(time, isPlaying);

        OnTimeUpdated?.Invoke(time);
    }

    private void Update()
    {
        if (isPlaying)
        {
            float currentTime = GetTime();
            OnTimeUpdated?.Invoke(currentTime);
            CheckForEvents(currentTime);
        }
    }
    public TimelineEvent AddEvent(float time)
    {
        string eventName = $"Event @ {time:F2}s";
        var newEvent = new TimelineEvent(time, eventName);
        timelineEvents.Add(newEvent);
        Debug.Log($"{eventName} zamanına yeni bir event eklendi.");
        return newEvent;
    }

    private void CheckForEvents(float currentTime)
    {
        foreach (var ev in timelineEvents)
        {
            if (currentTime >= ev.time && !ev.triggered)
            {
                Debug.Log($"Event Tetiklendi: {ev.eventName}");
                ev.triggered = true;
            }
        }
    }
    public float GetTime()
    {
        return timer ? timer.getCurrentTime() : 0f;
    }

    private void ResetEventsIfNeeded(float currentTime)
    {
        foreach (var ev in timelineEvents)
        {

            if (currentTime < ev.time && ev.triggered)
            {
                ev.Reset();
                Debug.Log($"Event '{ev.eventName}' geri sarma nedeniyle sıfırlandı.");
            }
        }
    }
}