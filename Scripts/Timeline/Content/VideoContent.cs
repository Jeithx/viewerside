using UnityEngine;
using UnityEngine.Video;

public class VideoContent : Content
{
    private VideoPlayer _player;
    private RenderTexture _rt;
    private string contentPath;

    public RenderTexture GetTexture() => _rt;

    public void playVideo()
    {
        if (_player == null) return;
        if (_player.isPrepared) _player.Play();
        else
        {
            _player.prepareCompleted += OnPreparedThenPlayOnce;
            _player.Prepare();
        }
    }

    public void stopVideo()
    {
        if (_player) _player.Pause();
    }

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

    public VideoContent(VideoPlayer videoPrefab, string path, float startingPoint)
    {
        if (!videoPrefab)
        {
            Debug.LogError("VideoContent: videoPrefab null.");
            return;
        }

        contentPath = path;
        startTime = startingPoint;

        _player = Object.Instantiate(videoPrefab);
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.source = VideoSource.Url;
        _player.url = "file:///" + path.Replace("\\", "/");

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
            double seconds = vp.length > 0.0
                ? vp.length
                : (vp.frameCount > 0 && vp.frameRate > 0 ? (double)vp.frameCount / vp.frameRate : 0.0);
            contentLength = Mathf.Max(0.001f, (float)seconds);
            vp.Pause();
        };

        _player.prepareCompleted += handler;
        _player.Prepare();
    }

    public VideoPlayer GetVideoPlayer() => _player;
    public string getPath() => contentPath;
}