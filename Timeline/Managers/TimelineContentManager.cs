using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using UnityEngine.UI;

public class TimelineContentManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private readonly List<VideoController> videoControllerList = new();
    private readonly List<ImageController> imageControllerList = new();
    private readonly List<AudioController> audioControllerList = new();
    private readonly List<ModelController> modelControllerList = new();

    private readonly Dictionary<Content, float> imageLengths = new();

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }

    public VideoContent AddVideoClip(string contentPath, VideoPlayer videoPrefab, GameObject barPrefab)
    {
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, timeline.headerPrefab, timeline.headersParent);
        int row = trackManager.GetAvailableTrackIndex();

        float newStart = Mathf.Max(
            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
            Mathf.Max(
                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

        var clip = new VideoContent(videoPrefab, barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[row], newStart, timeline);

        var host = new GameObject($"VideoController_{videoControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = VideoController.Create(host, timeline.GetTimer(), clip, timeline);
        videoControllerList.Add(controller);

        timeline.EnsureWidthForTime(newStart + 0.1f);
        timeline.ShowContent(clip, clip.GetTexture());
        timeline.HideContent(clip);
        StartCoroutine(RegisterBarWhenReady(clip));
        //timeline.UpdateRowCloseButtons();

        return clip;
    }

    public ImageContent AddImage(string contentPath, GameObject barPrefab)
    {
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, timeline.headerPrefab, timeline.headersParent);
        int row = trackManager.GetTrackRows().Count - 1;

        float newStart = Mathf.Max(
            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
            Mathf.Max(
                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

        var clip = new ImageContent(barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[row], newStart);

        var host = new GameObject($"ImageController_{imageControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = ImageController.Create(host, timeline.GetTimer(), clip, timeline);
        imageControllerList.Add(controller);

        timeline.ShowContent(clip, clip.GetTexture());
        timeline.HideContent(clip);
        if (clip.BarRT) timeline.GetBarManager().RegisterBar(clip.BarRT, clip, isImage: true);

        timeline.EnsureWidthForTime(newStart + clip.getLength());
        //timeline.UpdateRowCloseButtons();

        return clip;
    }

    public AudioContent AddAudio(string contentPath, GameObject barPrefab)
    {
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, timeline.headerPrefab, timeline.headersParent);
        int row = trackManager.GetTrackRows().Count - 1;

        float newStart = Mathf.Max(
            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
            Mathf.Max(
                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

        var clip = new AudioContent(barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[row], newStart, timeline);

        var host = new GameObject($"AudioController_{audioControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = AudioController.Create(host, timeline.GetTimer(), clip, timeline, contentPath);
        audioControllerList.Add(controller);

        if (clip.BarRT) timeline.GetBarManager().RegisterBar(clip.BarRT, clip, isImage: false);

        timeline.EnsureWidthForTime(newStart + 0.1f);
        //timeline.UpdateRowCloseButtons();

        return clip;
    }

    public ModelContent AddModelToTimeline(GameObject modelPrefab, GameObject barPrefab, GameObject contextMenuPrefab, float duration = 30f)
    {
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, timeline.headerPrefab, timeline.headersParent);
        int row = trackManager.GetTrackRows().Count - 1;

        float videoEnd = (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f;
        float imageEnd = (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f;
        float audioEnd = (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f;
        float modelEnd = (modelControllerList.Count > 0) ? modelControllerList[^1].getmc().getEnd() : 0f;
        float newModelStart = Mathf.Max(videoEnd, imageEnd, audioEnd, modelEnd);

        var modelContent = new ModelContent(
            modelPrefab,
            barPrefab,
            config.gridCellHorizontalPixelCount,
            contextMenuPrefab,
            trackManager.GetTrackRows()[row],
            newModelStart,
            duration);

        var host = new GameObject($"ModelController_{modelControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);

        var controller = ModelController.Create(host, timeline.GetTimer(), true, modelContent, timeline);
        modelControllerList.Add(controller);

        if (modelContent.BarRT) timeline.GetBarManager().RegisterBar(modelContent.BarRT, modelContent, isImage: false);

        timeline.EnsureWidthForTime(newModelStart + duration);
        //timeline.UpdateRowCloseButtons();

        return modelContent;
    }

    private System.Collections.IEnumerator RegisterBarWhenReady(VideoContent clip)
    {
        while (clip == null || clip.BarRT == null) yield return null;

        yield return new WaitForEndOfFrame();

        if (timeline.scrollViewContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(timeline.scrollViewContent);
        yield return null;

        timeline.GetBarManager().RegisterBar(clip.BarRT, clip, isImage: false);
        //timeline.UpdateRowCloseButtons();
    }

    public void ApplyAudioPreview(AudioContent audioContent)
    {
        if (audioContent != null)
        {
            StartCoroutine(ApplyAudioPreviewRoutine(audioContent));
        }
    }

    private IEnumerator ApplyAudioPreviewRoutine(AudioContent audioContent)
    {
        string path = audioContent.getPath();
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        if (clip == null) yield break;

        Texture2D previewTexture = null;
        int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            previewTexture = AssetPreview.GetAssetPreview(clip);
            if (previewTexture != null)
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        if (previewTexture != null && audioContent.BarRT != null)
        {
            Image barImage = audioContent.BarRT.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.color = Color.white;
                barImage.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), new Vector2(0.5f, 0.5f));
                barImage.type = Image.Type.Sliced;
            }
        }
        else
        {
            Debug.LogWarning("Ses dosyası için önizleme alınamadı: " + path);
        }
    }

    public void ClearAll()
    {
        foreach (var vc in videoControllerList)
            if (vc != null) Destroy(vc.gameObject);
        videoControllerList.Clear();

        foreach (var ic in imageControllerList)
            if (ic != null) Destroy(ic.gameObject);
        imageControllerList.Clear();

        foreach (var ac in audioControllerList)
            if (ac != null) Destroy(ac.gameObject);
        audioControllerList.Clear();

        foreach (var mc in modelControllerList)
            if (mc != null) Destroy(mc.gameObject);
        modelControllerList.Clear();

        imageLengths.Clear();
    }

    public Content GetLastAddedContent()
    {
        Content lastContent = null;
        float latestTime = -1;

        if (videoControllerList.Count > 0)
        {
            var vc = videoControllerList[videoControllerList.Count - 1].getvc();
            if (vc != null) { lastContent = vc; latestTime = 0; }
        }

        if (imageControllerList.Count > 0)
        {
            var ic = imageControllerList[imageControllerList.Count - 1].getic();
            if (ic != null) { lastContent = ic; latestTime = 0; }
        }

        if (audioControllerList.Count > 0)
        {
            var ac = audioControllerList[audioControllerList.Count - 1].getac();
            if (ac != null) { lastContent = ac; latestTime = 0; }
        }

        if (modelControllerList.Count > 0)
        {
            var mc = modelControllerList[modelControllerList.Count - 1].getmc();
            if (mc != null) { lastContent = mc; latestTime = 0; }
        }

        return lastContent;
    }

    public void SetLastImageDuration(float duration)
    {
        if (imageControllerList.Count > 0)
        {
            var lastController = imageControllerList[imageControllerList.Count - 1];
            var content = lastController.getic();
            if (content != null)
            {
                imageLengths[content] = duration;

                var barManager = timeline.GetBarManager();
                if (barManager.GetContentToBar().TryGetValue(content, out var bar))
                {
                    float width = timeline.TimeToX(duration) - timeline.TimeToX(0f);
                    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                }
            }
        }
    }

    public void AddVideoClipToSpecificTrack(string contentPath, VideoPlayer videoPrefab, GameObject barPrefab, int targetTrackIndex)
    {
        Debug.Log($"AddVideoClipToSpecificTrack: target track = {targetTrackIndex}");
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(targetTrackIndex + 1, config, timeline.headerPrefab, timeline.headersParent);

        var clip = new VideoContent(videoPrefab, barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[targetTrackIndex], 0f, timeline);

        var host = new GameObject($"VideoController_{videoControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = VideoController.Create(host, timeline.GetTimer(), clip, timeline);
        videoControllerList.Add(controller);

        timeline.ShowContent(clip, clip.GetTexture());
        timeline.HideContent(clip);
        StartCoroutine(RegisterBarWhenReady(clip));

        Debug.Log($"Video clip created on track {targetTrackIndex}");
    }

    public void AddImageToSpecificTrack(string contentPath, GameObject barPrefab, int targetTrackIndex)
    {
        Debug.Log($"AddImageToSpecificTrack: target track = {targetTrackIndex}");
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(targetTrackIndex + 1, config, timeline.headerPrefab, timeline.headersParent);

        var clip = new ImageContent(barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[targetTrackIndex], 0f);

        var host = new GameObject($"ImageController_{imageControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = ImageController.Create(host, timeline.GetTimer(), clip, timeline);
        imageControllerList.Add(controller);

        timeline.ShowContent(clip, clip.GetTexture());
        timeline.HideContent(clip);
        if (clip.BarRT) timeline.GetBarManager().RegisterBar(clip.BarRT, clip, isImage: true);

        Debug.Log($"Image clip created on track {targetTrackIndex}");
    }

    public void AddAudioToSpecificTrack(string contentPath, GameObject barPrefab, int targetTrackIndex)
    {
        Debug.Log($"AddAudioToSpecificTrack: target track = {targetTrackIndex}");
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(targetTrackIndex + 1, config, timeline.headerPrefab, timeline.headersParent);

        var clip = new AudioContent(barPrefab, contentPath,
            config.gridCellHorizontalPixelCount, trackManager.GetTrackRows()[targetTrackIndex], 0f, timeline);

        var host = new GameObject($"AudioController_{audioControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);
        var controller = AudioController.Create(host, timeline.GetTimer(), clip, timeline, contentPath);
        audioControllerList.Add(controller);

        if (clip.BarRT) timeline.GetBarManager().RegisterBar(clip.BarRT, clip, isImage: false);

        Debug.Log($"Audio clip created on track {targetTrackIndex}");
    }

    public void AddModelToSpecificTrack(GameObject modelPrefab, GameObject barPrefab, GameObject contextMenuPrefab, int targetTrackIndex, float duration = 30f)
    {
        Debug.Log($"AddModelToSpecificTrack: target track = {targetTrackIndex}");
        var trackManager = timeline.GetTrackManager();
        var config = timeline.GetConfig();

        trackManager.EnsureRowsAtLeast(targetTrackIndex + 1, config, timeline.headerPrefab, timeline.headersParent);

        var modelContent = new ModelContent(
            modelPrefab,
            barPrefab,
            config.gridCellHorizontalPixelCount,
            contextMenuPrefab,
            trackManager.GetTrackRows()[targetTrackIndex],
            0f,
            duration);

        var host = new GameObject($"ModelController_{modelControllerList.Count}");
        host.transform.SetParent(timeline.transform, false);

        var controller = ModelController.Create(host, timeline.GetTimer(), true, modelContent, timeline);
        modelControllerList.Add(controller);

        if (modelContent.BarRT) timeline.GetBarManager().RegisterBar(modelContent.BarRT, modelContent, isImage: false);

        Debug.Log($"Model clip created on track {targetTrackIndex}");
    }


    public float GetLengthOverride(Content c, float fallback) =>
        imageLengths.TryGetValue(c, out var v) ? v : fallback;

    public void SetImageLength(Content content, float duration) => imageLengths[content] = duration;

    public List<VideoController> GetVideoControllers() => videoControllerList;
    public List<ImageController> GetImageControllers() => imageControllerList;
    public List<AudioController> GetAudioControllers() => audioControllerList;
    public List<ModelController> GetModelControllers() => modelControllerList;
    public Dictionary<Content, float> GetImageLengths() => imageLengths;
}