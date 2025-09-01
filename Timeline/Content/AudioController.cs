using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AudioController : MonoBehaviour
{
    private AudioContent ac;
    private Timer timer;
    private TimelineGrid grid;

    private AudioSource _source;
    private string _path;
    private bool _ready;

    public AudioContent getac() => ac;

    // Create & init
    public static AudioController Create(GameObject host, Timer time, AudioContent content, TimelineGrid tlg, string filePath)
    {
        var ctrl = host.AddComponent<AudioController>();
        ctrl.Init(time, content, tlg, filePath);
        return ctrl;
    }

    public void Init(Timer time, AudioContent content, TimelineGrid tlg, string filePath)
    {
        timer = time;
        ac = content;
        grid = tlg;
        _path = filePath;

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;

        StartCoroutine(LoadClipCoroutine(_path));
    }

    private IEnumerator LoadClipCoroutine(string filePath)
    {
        string url = "file:///" + filePath.Replace("\\", "/");
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError($"[AudioController] Failed to load: {filePath} — {req.error}");
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(req);
        if (!clip)
        {
            Debug.LogError($"[AudioController] Null clip after load: {filePath}");
            yield break;
        }

        _source.clip = clip;
        _ready = true;

        // inform bar about the real length
        ac?.SetAudioClip(clip);
        grid.ApplyAudioPreview(ac);

    }

    // --- Timeline hooks ---
    public bool IsTimeInside(float t)
    {
        if (ac == null) return false;
        return t >= ac.getStart() && t <= ac.getEnd();
    }

    /// <summary>
    /// Jump the audio to 'timelineTime'. If shouldPlay==true, play; else pause at that position.
    /// </summary>
    public void ScrubTo(float timelineTime, bool shouldPlay)
    {
        if (!_ready || _source == null || ac == null || _source.clip == null) return;

        if (IsTimeInside(timelineTime))
        {
            float local = Mathf.Clamp(timelineTime - ac.getStart(), 0f, ac.getLength());

            // Only update time if it changed significantly to avoid stutter
            if (Mathf.Abs(_source.time - local) > 0.01f)
                _source.time = local;

            if (shouldPlay)
            {
                if (!_source.isPlaying) _source.Play();
            }
            else
            {
                if (_source.isPlaying) _source.Pause();
            }
        }
        else
        {
            if (_source.isPlaying) _source.Pause();
        }
    }

    public void ForceStop() { if (_source) _source.Pause(); }
    public void ResetState() { /* nothing for now */ }
}
