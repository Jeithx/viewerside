using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class TimelinePreviewManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private static Dictionary<string, Texture2D[]> videoFrameCache = new Dictionary<string, Texture2D[]>();
    private readonly Dictionary<Content, Coroutine> activeFrameCoroutines = new();

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }

    public void CreateBarPreviews(RectTransform barRT, Content content)
    {
        if (content is AudioContent)
        {
            return;
        }

        if (!barRT || content == null) return;

        GameObject containerGO = new GameObject("PreviewContainer", typeof(RectTransform));
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.SetParent(barRT, false);

        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        int dynamicPreviewCount = GetDynamicPreviewCount(barRT);
        float barWidth = barRT.rect.width;
        var config = timeline.GetConfig();
        float previewWidth = (barWidth - (config.previewPadding * (dynamicPreviewCount + 1))) / dynamicPreviewCount;

        if (previewWidth > 5f)
        {
            RawImage[] previewImages = new RawImage[dynamicPreviewCount];
            for (int i = 0; i < dynamicPreviewCount; i++)
            {
                CreateSinglePreview(containerRT, content, i, previewWidth, dynamicPreviewCount);
                previewImages[i] = containerRT.Find($"Preview_{i}").GetComponent<RawImage>();
            }

            if (content is VideoContent videoContent)
            {
                StartCoroutine(CaptureVideoFramesWithCache(videoContent, previewImages));
            }
        }
    }

    private void CreateSinglePreview(RectTransform container, Content content, int index, float previewWidth, int totalPreviews)
    {
        GameObject previewGO = new GameObject($"Preview_{index}", typeof(RectTransform), typeof(RawImage));
        RectTransform previewRT = previewGO.GetComponent<RectTransform>();
        RawImage previewImage = previewGO.GetComponent<RawImage>();

        previewRT.SetParent(container, false);

        var config = timeline.GetConfig();
        float xPos = config.previewPadding + (index * (previewWidth + config.previewPadding));

        previewRT.anchorMin = new Vector2(0, 0);
        previewRT.anchorMax = new Vector2(0, 1);
        previewRT.pivot = new Vector2(0, 0.5f);
        previewRT.anchoredPosition = new Vector2(xPos, 0);
        previewRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, previewWidth);
        previewRT.offsetMin = new Vector2(previewRT.offsetMin.x, 2f);
        previewRT.offsetMax = new Vector2(previewRT.offsetMax.x, -2f);

        previewImage.raycastTarget = false;

        if (content is ImageContent imageContent)
        {
            previewImage.texture = imageContent.GetTexture();
        }
        else if (content is ModelContent modelContent)
        {
            previewImage.color = new Color(0.3f, 0.3f, 0.8f, 0.5f);
        }
    }

    private int GetDynamicPreviewCount(RectTransform barRT)
    {
        if (!barRT) return 2;

        float barWidth = barRT.rect.width;
        var config = timeline.GetConfig();

        float zoomFactor = config.gridCellHorizontalPixelCount / 20f;
        int basePreviewCount = Mathf.RoundToInt(config.previewCount * Mathf.Sqrt(zoomFactor));

        int maxPreviewsByWidth = Mathf.FloorToInt((barWidth - config.previewPadding) / (30f + config.previewPadding));

        int finalCount = Mathf.Clamp(
            Mathf.Min(basePreviewCount, maxPreviewsByWidth),
            1,
            20
        );

        return finalCount;
    }

    private System.Collections.IEnumerator CaptureVideoFramesWithCache(VideoContent videoContent, RawImage[] previewImages)
    {
        string videoPath = videoContent.getPath();
        var config = timeline.GetConfig();
        config.previewCount = previewImages.Length;
        string cacheKey = $"{videoPath}_{config.previewCount}";

        if (videoFrameCache.TryGetValue(cacheKey, out Texture2D[] cachedFrames))
        {
            for (int i = 0; i < previewImages.Length && i < cachedFrames.Length; i++)
            {
                if (previewImages[i] != null && cachedFrames[i] != null)
                {
                    previewImages[i].texture = cachedFrames[i];
                }
            }
            yield break;
        }

        Coroutine captureCoroutine = StartCoroutine(CaptureVideoFrames(videoContent, previewImages));
        activeFrameCoroutines[videoContent] = captureCoroutine;

        yield return captureCoroutine;

        if (activeFrameCoroutines.ContainsKey(videoContent))
        {
            activeFrameCoroutines.Remove(videoContent);
        }

        Texture2D[] framesToCache = new Texture2D[previewImages.Length];
        bool anyFrameCaptured = false;
        for (int i = 0; i < previewImages.Length; i++)
        {
            if (previewImages[i] != null && previewImages[i].texture is Texture2D tex)
            {
                framesToCache[i] = tex;
                anyFrameCaptured = true;
            }
        }

        if (anyFrameCaptured)
        {
            videoFrameCache[cacheKey] = framesToCache;
        }
    }

    private System.Collections.IEnumerator CaptureVideoFrames(VideoContent videoContent, RawImage[] previewImages)
    {
        var videoPlayer = videoContent.GetVideoPlayer();
        if (!videoPlayer) yield break;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return new WaitForSeconds(0.1f);

        float videoDuration = videoContent.getLength();

        yield return StartCoroutine(CaptureFramesAsync(videoPlayer, videoDuration, previewImages));

        videoPlayer.Stop();
        videoPlayer.time = 0;
    }

    private System.Collections.IEnumerator CaptureFramesAsync(VideoPlayer player, float duration, RawImage[] previewImages)
    {
        for (int i = 0; i < previewImages.Length; i++)
        {
            if (previewImages[i] == null) continue;

            float frameTime = (duration / previewImages.Length) * i;

            player.time = frameTime;
            player.Play();

            yield return new WaitForSeconds(0.1f);

            if (player.texture != null)
            {
                previewImages[i].texture = DuplicateTextureFast(player.texture);
            }

            player.Pause();
            yield return null;
        }
    }

    private Texture2D DuplicateTextureFast(Texture source)
    {
        int width = Mathf.Min(source.width, 128);
        int height = Mathf.Min(source.height, 72);

        RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default);

        Graphics.Blit(source, tempRT);

        RenderTexture.active = tempRT;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);

        return result;
    }

    public void RefreshBarPreviews(RectTransform barRT, Content content)
    {
        if (activeFrameCoroutines.TryGetValue(content, out Coroutine runningCoroutine))
        {
            if (runningCoroutine != null)
            {
                StopCoroutine(runningCoroutine);
            }
            activeFrameCoroutines.Remove(content);
        }

        Transform oldContainer = barRT.Find("PreviewContainer");
        if (oldContainer)
            DestroyImmediate(oldContainer.gameObject);

        if (timeline.GetConfig().enablePreviews)
            CreateBarPreviews(barRT, content);
    }

    public static void ClearVideoFrameCache()
    {
        foreach (var frames in videoFrameCache.Values)
        {
            foreach (var frame in frames)
            {
                if (frame != null) Destroy(frame);
            }
        }
        videoFrameCache.Clear();
    }

    public void StopCoroutineForContent(Content content)
    {
        if (activeFrameCoroutines.TryGetValue(content, out var coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
            activeFrameCoroutines.Remove(content);
        }
    }
}