using UnityEngine;

public class ImageController : MonoBehaviour
{
    private ImageContent ic;
    private Timer timer;
    private TimelineWindowManager windowManager; //instead of timelinegrid we are using window manager

    [SerializeField] private float epsilon = 0.05f;
    private bool started;
    private bool finished;
    private bool isPlaying = false; //play state tracking

    public static ImageController Create(
        GameObject host,
        Timer time,
        ImageContent imageContent,
        TimelineWindowManager windowMgr)
    {
        var ctrl = host.AddComponent<ImageController>();
        ctrl.Init(time, imageContent, windowMgr);
        return ctrl;
    }

    public void Init(Timer time, ImageContent imc, TimelineWindowManager windowMgr)
    {
        timer = time;
        ic = imc;
        windowManager = windowMgr;
        started = false;
        finished = false;
    }

    public ImageContent getic() => ic;

    private void Update()
    {
        if (!timer || !isPlaying) return; 

        float t = timer.getCurrentTime();

        float length = ic.getLength();
        float start = ic.getStart();
        float end = start + length;

        if (!started && t >= start - epsilon && t <= end + epsilon)
        {
            started = true;
            finished = false;
            if (windowManager != null)
                windowManager.ShowContent(ic, ic.GetTexture());
        }

        if (started && !finished && t >= end - epsilon)
        {
            finished = true;
            if (windowManager != null)
                windowManager.HideContent(ic);
        }
    }

    public void ScrubTo(float globalTime, bool shouldPlay)
    {
        if (ic == null) return;

        isPlaying = shouldPlay;

        float length = ic.getLength();
        float start = ic.getStart();
        float end = start + length;

        if (globalTime < start - epsilon || globalTime > end + epsilon)
        {
            started = false;
            finished = false;
            if (windowManager != null)
                windowManager.HideContent(ic);
            return;
        }

        if (windowManager != null)
            windowManager.ShowContent(ic, ic.GetTexture());
        finished = false;

        started = true;
    }

    public void ResetState()
    {
        started = false;
        finished = false;
        isPlaying = false;
    }

    public void SetPlayState(bool playing)
    {
        isPlaying = playing;
    }
}