using UnityEngine;
using UnityEngine.UI;

public class AudioContent : Content
{

    private string _filePath;

    private AudioClip _audioClip;
    public RectTransform BarRT { get; private set; }

    public string getPath() => _filePath;

    public Texture2D WaveformTexture { get; set; }


    public AudioContent(
        GameObject barPrefab,
        string path,                    // not used here, but kept for parity
        int timeToPixel,                // not used directly; we call grid helpers
        RectTransform rowParent,
        float startingPoint)    {
        startTime = startingPoint;
        contentLength = 1f; // temporary until controller loads the clip
        _filePath = path;
    }

    // --- API used by TimelineGrid / AudioController ---
    public new float getStart() => startTime;
    public new float getEnd() => startTime + contentLength;
    public new float getLength() => contentLength;

    public new void SetStart(float newStart)
    {
        startTime = Mathf.Max(0f, newStart);
    }

    public AudioClip GetAudioClip()
    {
        return _audioClip;
    }

    public void SetAudioClip(AudioClip clip)
    {
        _audioClip = clip;
        if (clip != null)
        {
            SetLength(clip.length);
        }
        else
        {
            Debug.LogWarning("[AudioContent] SetAudioClip received null clip");
        }
    }

    public void SetLength(float seconds)
    {
        contentLength = Mathf.Max(0.01f, seconds);
    }
}
