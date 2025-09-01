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
    }

    public void SetTime(float time)
    {
        time = Mathf.Max(0f, time);
        timer?.SetCurrentTime(time);

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

    public float GetTime()
    {
        return timer ? timer.getCurrentTime() : 0f;
    }
}