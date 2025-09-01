using UnityEngine;

public class ModelController : MonoBehaviour
{
    private ModelContent mc;
    private Timer timer;
    private TimelineGrid timeline;

    [SerializeField] private float epsilon = 0.05f;
    private bool started;
    private bool finished;
    private float lastUpdateTime = -1f;

    public static ModelController Create(
        GameObject host,
        Timer time,
        bool _,
        ModelContent modelContent,
        TimelineGrid tlg)
    {
        var ctrl = host.AddComponent<ModelController>();
        ctrl.Init(time, modelContent, tlg);
        return ctrl;
    }

    public void Init(Timer time, ModelContent modelContent, TimelineGrid tlg)
    {
        timer = time;
        mc = modelContent;
        timeline = tlg;
        started = false;
        finished = false;
    }

    public ModelContent getmc() => mc;

    private void Update()
    {
        if (!timer || !timeline) return;
        if (!timeline.getFlag()) return; // Works only in play mode

        float t = timer.getCurrentTime();
        float start = mc.getStart();
        float end = mc.getEnd();

        // Check if model should be shown
        if (t >= start - epsilon && t <= end + epsilon)
        {
            if (!started)
            {
                started = true;
                finished = false;
                mc.Show();
            }

            // Update animation with proper time
            float localTime = t - start;
            mc.SetAnimationTime(localTime, true); // true = playing
            mc.UpdateAnimation(localTime);

            // Hide model if animation is finished
            if (localTime >= mc.getLength())
            {
                finished = true;
                mc.Hide();
                started = false;
            }
        }
        else if (started && !finished && t > end + epsilon)
        {
            // We passed the model showtime
            finished = true;
            mc.Hide();
            mc.ResetAnimation();
        }
        else if (t < start - epsilon && started)
        {
            // We rolled it back
            started = false;
            mc.Hide();
            mc.ResetAnimation();
        }
    }

    // Called during scrubbing
    public void ScrubTo(float globalTime, bool shouldPlay)
    {
        Debug.Log($"Scrubbing model at global time: {globalTime:F2}s, shouldPlay: {shouldPlay}");
        if (mc == null)
        {
            Debug.LogWarning("ModelContent is null in ModelController.ScrubTo");
            return;
        }


        float start = mc.getStart();
        float end = mc.getEnd();

        // Outside time range - hide model
        if (globalTime < start - epsilon || globalTime > end + epsilon)
        {
            mc.Hide();
            started = false;
            finished = false;
            mc.ResetAnimation();
            lastUpdateTime = -1f; // Reset last time
            return;
        }

        // Within time range - show model and set animation frame
        float local = Mathf.Clamp(globalTime - start, 0f, mc.getLength());

        // Only update if time has actually changed (even by a tiny amount)
        bool timeChanged = Mathf.Abs(local - lastUpdateTime) > 0.001f;

        mc.Show();

        // ALWAYS update animation frame during scrubbing, regardless of time change
        if (!shouldPlay || timeChanged)
        {
            mc.SetAnimationTime(local, shouldPlay);
            lastUpdateTime = local;
        }

        // Update state flags
        started = true;
        finished = false;

        // Less verbose logging
        if (timeChanged)
        {
            Debug.Log($"Scrubbing model to local time: {local:F2}s (global: {globalTime:F2}s), playing: {shouldPlay}");
        }
    }

    public void ForceStop()
    {
        if (mc != null)
        {
            mc.Hide();
            mc.ResetAnimation();
        }
    }

    public void ResetState()
    {
        started = false;
        finished = false;
        lastUpdateTime = -1f;
        if (mc != null)
            mc.ResetAnimation();
    }
}