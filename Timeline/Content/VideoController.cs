using UnityEngine;

public class VideoController : MonoBehaviour
{
    private VideoContent vc;
    private Timer timer;
    private TimelineGrid timeline;

    [SerializeField] private float epsilon = 0.05f;
    private bool started;
    private bool finished;

    public static VideoController Create(
        GameObject host,
        Timer time,
        VideoContent videoContent,
        TimelineGrid tlg)
    {
        var ctrl = host.AddComponent<VideoController>();
        ctrl.Init(time, videoContent, tlg);
        return ctrl;
    }

    public void Init(Timer time, VideoContent videoContent, TimelineGrid tlg)
    {
        timer = time;
        vc = videoContent;
        timeline = tlg;
        started = false;
        finished = false;
    }

    public VideoContent getvc() => vc;

    private void Update()
    {
        if (!timer || !timeline) { 
            Debug.LogWarning("VideoController missing timer or timeline reference.");
        return; }
        if (!timeline.getFlag())
        {
            //Debug.Log("Timeline flag not set, skipping update.");
            return;
        }

        float t = timer.getCurrentTime();
        float start = vc.getStart();
        float end = vc.getEnd();

        if (!started && t >= start - epsilon && t <= end + epsilon)
        {
            Debug.Log("Attempting to start video");
            started = true;
            finished = false;
            timeline.ShowContent(vc, vc.GetTexture());  // layered window
            vc.playVideo();
        }
        else
        {
            Debug.Log("Not starting video: started=" + started + " t=" + t + " start=" + start + " end=" + end);
        }

        if (started && !finished && t >= end - epsilon)
        {
            finished = true;
            vc.stopVideo();
            timeline.HideContent(vc);
        }
    }

    public void ScrubTo(float globalTime, bool shouldPlay)
    {
        if (vc == null) return;

        float start = vc.getStart();
        float end = vc.getEnd();

        if (globalTime < start - epsilon || globalTime > end + epsilon)
        {
            vc.stopVideo();
            started = false;
            finished = false;
            timeline.HideContent(vc);
            return;
        }

        timeline.ShowContent(vc, vc.GetTexture());

        float local = Mathf.Clamp(globalTime - start, 0f, vc.getLength());
        vc.SeekToSeconds(local, shouldPlay);

        started = shouldPlay || local > 0f;
        finished = false;
    }

    public void ForceStop()
    {
        if (vc != null) vc.stopVideo();
        if (timeline != null) timeline.HideContent(vc);
    }

    private void Awake()
    {
        Debug.Log("[VideoController] Awake on obj " + this.name);
    }
    public void ResetState()
    {
        started = false;
        finished = false;
    }
}
