using UnityEngine;

public class ImageController : MonoBehaviour
{
    private ImageContent ic;
    private Timer timer;
    private TimelineGrid timeline;

    [SerializeField] private float epsilon = 0.05f;
    private bool started;
    private bool finished;

    public static ImageController Create(
        GameObject host,
        Timer time,
        ImageContent imageContent,
        TimelineGrid tlg)
    {
        var ctrl = host.AddComponent<ImageController>();
        ctrl.Init(time, imageContent, tlg);
        return ctrl;
    }

    public void Init(Timer time, ImageContent imc, TimelineGrid tlg)
    {
        timer = time;
        ic = imc;
        timeline = tlg;
        started = false;
        finished = false;
    }

    public ImageContent getic() => ic;

    private void Update()
    {
        if (!timer || !timeline) return;
        if (!timeline.getFlag()) return;

        float t = timer.getCurrentTime();

        // Honor bar-resized duration if present
        float length = timeline.GetLengthOverride(ic, ic.getLength());
        float start = ic.getStart();
        float end = start + length;

        if (!started && t >= start - epsilon && t <= end + epsilon)
        {
            started = true;
            finished = false;
            timeline.ShowContent(ic, ic.GetTexture());
        }

        if (started && !finished && t >= end - epsilon)
        {
            finished = true;
            timeline.HideContent(ic);
        }
    }

    public void ScrubTo(float globalTime, bool shouldPlay)
    {
        if (ic == null || timeline == null) return;

        float length = timeline.GetLengthOverride(ic, ic.getLength());
        float start = ic.getStart();
        float end = start + length;

        if (globalTime < start - epsilon || globalTime > end + epsilon)
        {
            started = false;
            finished = false;
            timeline.HideContent(ic);
            return;
        }

        timeline.ShowContent(ic, ic.GetTexture());
        finished = false;
    }

    public void ResetState()
    {
        started = false;
        finished = false;
    }
}
