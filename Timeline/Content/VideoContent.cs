using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoContent : Content
{
    private VideoPlayer _player;
    private bool _barBuilt;
    private RenderTexture _rt;
    private TimelineGrid _grid;
    private string contentPath;
    public RenderTexture GetTexture() => _rt;
    public RectTransform BarRT { get; private set; }

    public void playVideo()
    {
        Debug.Log("VideoContent: playVideo called.");
        if (_player == null) return;
        if (_player.isPrepared) { _player.Play(); return; }
        _player.prepareCompleted += OnPreparedThenPlayOnce;
        _player.Prepare();
    }
    public void stopVideo() { if (_player) _player.Pause(); }

    public void SeekToSeconds(double localSeconds, bool playAfter)
    {
        if (_player == null) return;
        double clamped = Mathf.Clamp((float)localSeconds, 0f, contentLength);

        if (!_player.isPrepared)
        {
            _player.prepareCompleted += (vp) =>
            {
                _player.time = clamped;
                if (playAfter) _player.Play();
                else { _player.Play(); _player.Pause(); }
            };
            _player.Prepare();
            return;
        }

        _player.time = clamped;
        if (playAfter) _player.Play();
        else { _player.Play(); _player.Pause(); }
    }

    private void OnPreparedThenPlayOnce(VideoPlayer vp)
    {
        _player.prepareCompleted -= OnPreparedThenPlayOnce;
        _player.Play();
    }

    public VideoContent(
        VideoPlayer videoPrefab,
        GameObject barPrefab,
        string path,
        int timeToPixel,
        RectTransform parentRow,     // parent is a ROW
        float startingPoint, TimelineGrid grid)
    {
        if (!videoPrefab || !barPrefab) { Debug.LogError("VideoContent: missing prefabs."); return; }
        if (!parentRow) { Debug.LogError("VideoContent: parentRow null."); return; }


        contentPath = path;
        panel = barPrefab;
        startTime = startingPoint;
        _grid = grid;
        _player = Object.Instantiate(videoPrefab);
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.source = VideoSource.Url;
        _player.url = "file:///" + path.Replace("\\", "/");

        int w = (_player.targetTexture != null) ? _player.targetTexture.width : 1280;
        int h = (_player.targetTexture != null) ? _player.targetTexture.height : 720;
        _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        { name = "RT_" + System.IO.Path.GetFileName(path) };
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = _rt;

        // Build the bar after length is known
        VideoPlayer.EventHandler handler = null;
        handler = (vp) =>
        {
            _player.prepareCompleted -= handler;
            if (_barBuilt) return;
            _barBuilt = true;

            double seconds = vp.length > 0.0
                ? vp.length
                : (vp.frameCount > 0 && vp.frameRate > 0 ? (double)vp.frameCount / vp.frameRate : 0.0);

            contentLength = Mathf.Max(0.001f, (float)seconds);

            var barRoot = Object.Instantiate(barPrefab);
            var rootRT = barRoot.GetComponent<RectTransform>() ?? barRoot.AddComponent<RectTransform>();
            rootRT.SetParent(parentRow, false);

            RectTransform rt =
                barRoot.transform.childCount > 0
                    ? barRoot.transform.GetChild(0).GetComponent<RectTransform>()
                    : rootRT;

            if (!rt) { Debug.LogError("VideoContent: bar prefab has no RectTransform."); return; }
            BarRT = rt;

            rt.SetParent(parentRow, false);
            rt.localScale = Vector3.one;

            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);

            float width = (contentLength / 4f) * timeToPixel;
            float posX = (startTime / 4f) * timeToPixel;

            rt.anchoredPosition = new Vector2(posX, 0f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);

            var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.color = Color.red; // video = red
            img.raycastTarget = true;
            _grid?.EnsureWidthForTime(startTime + contentLength);
            vp.Pause();
        };

        _player.prepareCompleted += handler;
        _player.Prepare();
    }

    public VideoPlayer GetVideoPlayer()
    {
        return _player;
    }

    public string getPath()
    {
        return contentPath;
    }
}
