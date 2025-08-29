using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoContent : Content
{
    private VideoPlayer _player;
    private bool _barBuilt;
    private RenderTexture _rt;
    private string contentPath;

    public RenderTexture GetTexture() => _rt;
    public RectTransform BarRT { get; private set; }

    // Start playing the video
    public void playVideo()
    {
        Debug.Log("VideoContent: playVideo called.");
        if (_player == null) return;

        if (_player.isPrepared)
        {
            _player.Play();
            return;
        }

        _player.prepareCompleted += OnPreparedThenPlayOnce;
        _player.Prepare();
    }

    // Stop the video
    public void stopVideo()
    {
        if (_player) _player.Pause();
    }

    // Jump to specific time in video - useful for scrubbing
    public void SeekToSeconds(double localSeconds, bool playAfter)
    {
        if (_player == null) return;
        double clamped = Mathf.Clamp((float)localSeconds, 0f, contentLength);

        if (!_player.isPrepared)
        {
            // Video not ready yet, wait then seek
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
        else { _player.Play(); _player.Pause(); } // Play then pause = seek to frame
    }

    // Callback for when video is prepared
    private void OnPreparedThenPlayOnce(VideoPlayer vp)
    {
        _player.prepareCompleted -= OnPreparedThenPlayOnce;
        _player.Play();
    }

    // Constructor - sets up video player and render texture
    public VideoContent(
        VideoPlayer videoPrefab,
        GameObject barPrefab,
        string path,
        int timeToPixel,
        RectTransform parentRow,
        float startingPoint)
    {
        // Basic null checks because we're not savages
        if (!videoPrefab || !barPrefab)
        {
            Debug.LogError("VideoContent: missing prefabs.");
            return;
        }
        if (!parentRow)
        {
            Debug.LogError("VideoContent: parentRow null.");
            return;
        }

        contentPath = path;
        panel = barPrefab;
        startTime = startingPoint;

        // Create video player instance
        _player = Object.Instantiate(videoPrefab);
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.source = VideoSource.Url;
        _player.url = "file:///" + path.Replace("\\", "/");

        // Create render texture for video output
        int w = (_player.targetTexture != null) ? _player.targetTexture.width : 1280;
        int h = (_player.targetTexture != null) ? _player.targetTexture.height : 720;
        _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            name = "RT_" + System.IO.Path.GetFileName(path)
        };
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = _rt;

        VideoPlayer.EventHandler handler = null;
        handler = (vp) =>
        {
            _player.prepareCompleted -= handler;
            if (_barBuilt) return;
            _barBuilt = true;

            // Get video duration
            double seconds = vp.length > 0.0
                ? vp.length
                : (vp.frameCount > 0 && vp.frameRate > 0 ? (double)vp.frameCount / vp.frameRate : 0.0);

            contentLength = Mathf.Max(0.001f, (float)seconds);

            vp.Pause(); // Don't auto-play
        };

        _player.prepareCompleted += handler;
        _player.Prepare();
    }


    public VideoPlayer GetVideoPlayer() => _player;
    public string getPath() => contentPath;
}
