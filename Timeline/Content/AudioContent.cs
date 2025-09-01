using UnityEngine;
using UnityEngine.UI;

public class AudioContent : Content
{

    private string _filePath;

    private AudioClip _audioClip;
    public RectTransform BarRT { get; private set; }

    public string getPath() => _filePath;

    private readonly TimelineGrid _grid;
    public Texture2D WaveformTexture { get; set; }


    public AudioContent(
        GameObject barPrefab,
        string path,                    // not used here, but kept for parity
        int timeToPixel,                // not used directly; we call grid helpers
        RectTransform rowParent,
        float startingPoint,
        TimelineGrid grid)
    {
        _grid = grid;
        startTime = startingPoint;
        contentLength = 1f; // temporary until controller loads the clip
        _filePath = path;

        // Build bar
        var barRoot = Object.Instantiate(barPrefab, rowParent, false);
        var le = barRoot.GetComponent<LayoutElement>() ?? barRoot.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        BarRT = barRoot.GetComponent<RectTransform>();
        BarRT.anchorMin = new Vector2(0f, 0.5f);
        BarRT.anchorMax = new Vector2(0f, 0.5f);
        BarRT.pivot = new Vector2(0f, 0.5f);

        BarRT.anchoredPosition = new Vector2(_grid.TimeToX(startingPoint), 0f);
        BarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _grid.TimeToX(contentLength));
        BarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);

        var img = barRoot.GetComponent<Image>() ?? barRoot.AddComponent<Image>();
        img.color = new Color(1f, 0.9f, 0.2f, 0.95f); // yellow
        img.raycastTarget = true;
    }

    // --- API used by TimelineGrid / AudioController ---
    public new float getStart() => startTime;
    public new float getEnd() => startTime + contentLength;
    public new float getLength() => contentLength;

    public new void SetStart(float newStart)
    {
        startTime = Mathf.Max(0f, newStart);
        if (BarRT) BarRT.anchoredPosition = new Vector2(_grid.TimeToX(startTime), 0f);
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
        if (BarRT) BarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _grid.TimeToX(contentLength));
    }
}
