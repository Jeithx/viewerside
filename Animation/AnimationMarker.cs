[System.Serializable]
public class AnimationMarker
{
    public float timeOffset;
    public string animationName;
    public bool hasTriggered;
    public float animationDuration;

    public AnimationMarker(float time, string animName, float duration = 1f)
    {
        timeOffset = time;
        animationName = animName;
        animationDuration = duration;
        hasTriggered = false;
    }

    public void Reset()
    {
        hasTriggered = false;
    }

    public bool ShouldTrigger(float currentLocalTime)
    {
        return !hasTriggered && currentLocalTime >= timeOffset;
    }

    public bool IsActive(float currentLocalTime)
    {
        return currentLocalTime >= timeOffset && currentLocalTime <= (timeOffset + animationDuration);
    }
}