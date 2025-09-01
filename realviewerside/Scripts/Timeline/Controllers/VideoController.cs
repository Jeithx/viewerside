using UnityEngine;

public class VideoController : MonoBehaviour
{
    private VideoContent vc;
    private Timer timer;
    private TimelineWindowManager windowManager; // YENİ: timeline yerine windowManager
    private bool isPlaying = false; // YENİ: play state tracking

    [SerializeField] private float epsilon = 0.05f;
    private bool started;
    private bool finished;

    public static VideoController Create(
        GameObject host,
        Timer time,
        VideoContent videoContent,
        TimelineWindowManager windowMgr) // YENİ: TimelineGrid yerine TimelineWindowManager
    {
        var ctrl = host.AddComponent<VideoController>();
        ctrl.Init(time, videoContent, windowMgr);
        return ctrl;
    }

    public void Init(Timer time, VideoContent videoContent, TimelineWindowManager windowMgr)
    {
        timer = time;
        vc = videoContent;
        windowManager = windowMgr; // YENİ: windowManager'ı sakla
        started = false;
        finished = false;
    }

    public VideoContent getvc() => vc;

    private void Update()
    {
        if (!timer || !isPlaying) return; // YENİ: timeline.getFlag() yerine isPlaying

        float t = timer.getCurrentTime();
        float start = vc.getStart();
        float end = vc.getEnd();

        if (!started && t >= start - epsilon && t <= end + epsilon)
        {
            Debug.Log("Attempting to start video");
            started = true;
            finished = false;
            // YENİ: windowManager kullan
            if (windowManager != null)
                windowManager.ShowContent(vc, vc.GetTexture());
            vc.playVideo();
        }

        if (started && !finished && t >= end - epsilon)
        {
            finished = true;
            vc.stopVideo();
            // YENİ: windowManager kullan
            if (windowManager != null)
                windowManager.HideContent(vc);
        }
    }

    public void ScrubTo(float globalTime, bool shouldPlay)
    {
        if (vc == null) return;

        isPlaying = shouldPlay; // YENİ: play state'i güncelle

        float start = vc.getStart();
        float end = vc.getEnd();

        if (globalTime < start - epsilon || globalTime > end + epsilon)
        {
            vc.stopVideo();
            started = false;
            finished = false;
            // YENİ: windowManager kullan
            if (windowManager != null)
                windowManager.HideContent(vc);
            return;
        }

        // YENİ: windowManager kullan
        if (windowManager != null)
            windowManager.ShowContent(vc, vc.GetTexture());

        float local = Mathf.Clamp(globalTime - start, 0f, vc.getLength());
        vc.SeekToSeconds(local, shouldPlay);

        started = shouldPlay || local > 0f;
        finished = false;
    }

    public void ForceStop()
    {
        if (vc != null) vc.stopVideo();
        // YENİ: windowManager kullan
        if (windowManager != null) windowManager.HideContent(vc);
        isPlaying = false; // YENİ
    }

    public void ResetState()
    {
        started = false;
        finished = false;
        isPlaying = false; // YENİ: play state'i de reset et
    }

    // YENİ: Play/Stop control için
    public void SetPlayState(bool playing)
    {
        isPlaying = playing;
    }
}