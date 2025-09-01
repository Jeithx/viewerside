using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Globalization;

public class TimelineXMLSerializer : MonoBehaviour
{
    [Header("References")]
    public TimelineGrid timelineGrid;
    public TimelineConfig config;

    [Header("Model Registry")]
    public List<ModelEntry> modelRegistry = new List<ModelEntry>();

    [System.Serializable]
    public class ModelEntry
    {
        public string name;
        public GameObject prefab;
    }

    [System.Serializable]
    public class WindowData
    {
        public float posX;
        public float posY;
        public float width;
        public float height;
        public float anchorMinX = 0.5f;
        public float anchorMinY = 0.5f;
        public float anchorMaxX = 0.5f;
        public float anchorMaxY = 0.5f;
        public float pivotX = 0.5f;
        public float pivotY = 0.5f;
    }

    // ==================== EXPORT ====================

    public void ExportToXML(string filePath)
    {
        try
        {
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                CreateProjectElement()
            );

            doc.Save(filePath);
            Debug.Log($"Timeline exported to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Export failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private XElement CreateProjectElement()
    {
        var project = new XElement("TimelineProject",
            new XAttribute("version", "3.1"), //this is just for show for now
            new XAttribute("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new XAttribute("resolution", $"{Screen.width}x{Screen.height}")
        );

        if (config != null)
        {
            project.Add(new XElement("Settings",
                new XAttribute("gridCellPixels", config.gridCellHorizontalPixelCount),
                new XAttribute("laneHeight", config.laneHeight),
                new XAttribute("snapThreshold", config.snapPixelThreshold),
                new XAttribute("enablePreviews", config.enablePreviews)
            ));
        }

        var allClips = new List<ClipExportData>();

        var videoControllers = timelineGrid.GetVideoControllers();
        foreach (var controller in videoControllers)
        {
            var content = controller.getvc();
            if (content != null)
            {
                allClips.Add(new ClipExportData
                {
                    Content = content,
                    Type = "Video",
                    Controller = controller,
                    Track = GetContentTrack(content)
                });
            }
        }

        var imageControllers = timelineGrid.GetImageControllers();
        foreach (var controller in imageControllers)
        {
            var content = controller.getic();
            if (content != null)
            {
                allClips.Add(new ClipExportData
                {
                    Content = content,
                    Type = "Image",
                    Controller = controller,
                    Track = GetContentTrack(content)
                });
            }
        }

        var audioControllers = timelineGrid.GetAudioControllers();
        foreach (var controller in audioControllers)
        {
            var content = controller.getac();
            if (content != null)
            {
                allClips.Add(new ClipExportData
                {
                    Content = content,
                    Type = "Audio",
                    Controller = controller,
                    Track = GetContentTrack(content)
                });
            }
        }

        //var modelControllers = timelineGrid.GetContentManager().GetModelControllers(); //if we witness a bug here, revert to this line
        var modelControllers = timelineGrid.modelControllerList;
        foreach (var controller in modelControllers)
        {
            var content = controller.getmc();
            if (content != null)
            {
                allClips.Add(new ClipExportData
                {
                    Content = content,
                    Type = "Model",
                    Controller = controller,
                    Track = GetContentTrack(content)
                });
            }
        }

        var tracks = allClips.GroupBy(c => c.Track).OrderBy(g => g.Key);
        var tracksElement = new XElement("Tracks");
        foreach (var trackGroup in tracks)
        {
            var trackElement = new XElement("Track", new XAttribute("id", trackGroup.Key));
            var clipsElement = new XElement("Clips");
            foreach (var clipData in trackGroup.OrderBy(c => c.Content.getStart()))
            {
                clipsElement.Add(CreateClipElement(clipData));
            }
            trackElement.Add(clipsElement);
            tracksElement.Add(trackElement);
        }

        project.Add(tracksElement);
        return project;
    }

    private class ClipExportData
    {
        public Content Content;
        public string Type;
        public object Controller;
        public int Track;
    }

    private int GetContentTrack(Content content)
    {
        var barManager = timelineGrid.GetBarManager();
        var trackManager = timelineGrid.GetTrackManager();

        if (barManager != null && trackManager != null && barManager.GetContentToBar().TryGetValue(content, out var bar))
        {
            return trackManager.GetRowIndexOf(bar.parent as RectTransform);
        }
        return content.GetLayer(); // Fallback
    }

    private XElement CreateClipElement(ClipExportData clipData)
    {
        var content = clipData.Content;
        var clip = new XElement("Clip");

        clip.Add(new XAttribute("type", clipData.Type));
        clip.Add(new XAttribute("start", content.getStart().ToString("F3")));
        clip.Add(new XAttribute("track", clipData.Track));

        switch (clipData.Type)
        {
            case "Video":
                clip.Add(new XAttribute("path", (content as VideoContent).getPath()));
                break;
            case "Image":
                var ic = content as ImageContent;
                clip.Add(new XAttribute("path", ic.getPath()));
                float imageDuration = timelineGrid.GetLengthOverride(ic, ic.getLength());
                clip.Add(new XAttribute("duration", imageDuration.ToString("F3")));
                break;
            case "Audio":
                clip.Add(new XAttribute("path", (content as AudioContent).getPath()));
                break;
            case "Model":
                var mc = content as ModelContent;
                var prefabField = typeof(ModelContent).GetField("_modelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                GameObject prefab = prefabField?.GetValue(mc) as GameObject;
                if (prefab != null)
                {
                    clip.Add(new XAttribute("prefabName", GetModelName(prefab)));
                }
                string animName = mc.GetInitialAnimationName();
                if (!string.IsNullOrEmpty(animName))
                {
                    clip.Add(new XAttribute("animation", animName));
                }
                break;
        }

        // this now uses window manager to get window data SOMEHOW?!
        var windowData = GetWindowData(content);
        if (windowData != null)
        {
            var windowElement = new XElement("Window",
                new XAttribute("posX", windowData.posX.ToString("F2")),
                new XAttribute("posY", windowData.posY.ToString("F2")),
                new XAttribute("width", windowData.width.ToString("F2")),
                new XAttribute("height", windowData.height.ToString("F2")),
                new XAttribute("anchorMinX", windowData.anchorMinX.ToString("F3")),
                new XAttribute("anchorMinY", windowData.anchorMinY.ToString("F3")),
                new XAttribute("anchorMaxX", windowData.anchorMaxX.ToString("F3")),
                new XAttribute("anchorMaxY", windowData.anchorMaxY.ToString("F3")),
                new XAttribute("pivotX", windowData.pivotX.ToString("F3")),
                new XAttribute("pivotY", windowData.pivotY.ToString("F3"))
            );
            clip.Add(windowElement);
        }

        return clip;
    }

    private WindowData GetWindowData(Content content)
    {
        var windowManager = timelineGrid.GetWindowManager();
        if (windowManager == null || !windowManager.WindowExistsFor(content))
        {
            return null;
        }

        RectTransform rootRT = windowManager.GetWindowRoot(content);
        if (rootRT != null)
        {
            return new WindowData
            {
                posX = rootRT.anchoredPosition.x,
                    posY = rootRT.anchoredPosition.y,
                    width = rootRT.rect.width,
                    height = rootRT.rect.height,
                    anchorMinX = rootRT.anchorMin.x,
                    anchorMinY = rootRT.anchorMin.y,
                    anchorMaxX = rootRT.anchorMax.x,
                    anchorMaxY = rootRT.anchorMax.y,
                    pivotX = rootRT.pivot.x,
                    pivotY = rootRT.pivot.y
                };
            }
            return null;
    }


    // ==================== IMPORT ====================

    public void ImportFromXML(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(filePath);
            var root = doc.Root;

            if (root.Name != "TimelineProject")
            {
                Debug.LogError("Invalid XML format");
                return;
            }

            string savedResolution = root.Attribute("resolution")?.Value;
            if (!string.IsNullOrEmpty(savedResolution))
            {
                Debug.Log($"XML was created at resolution: {savedResolution}, current: {Screen.width}x{Screen.height}");
            }

            timelineGrid.ClearAll();
            StartCoroutine(ImportAfterClear(root));
        }
        catch (Exception e)
        {
            Debug.LogError($"Import failed: {e.Message}\n{e.StackTrace}");
        }
    }


    private IEnumerator ImportAfterClear(XElement root)
    {
        yield return new WaitForEndOfFrame();

        var settingsElement = root.Element("Settings");
        if (settingsElement != null && config != null)
        {
            config.gridCellHorizontalPixelCount = GetIntAttribute(settingsElement, "gridCellPixels", 20);
            config.laneHeight = GetFloatAttribute(settingsElement, "laneHeight", 60f);
            config.snapPixelThreshold = GetFloatAttribute(settingsElement, "snapThreshold", 10f);
            config.enablePreviews = GetBoolAttribute(settingsElement, "enablePreviews", true);
        }

        var tracks = root.Element("Tracks")?.Elements("Track");
        if (tracks != null)
        {
            yield return StartCoroutine(ImportClipsWithTracks(tracks));
        }

        Debug.Log($"Timeline imported successfully");
    }

    private IEnumerator ImportClipsWithTracks(IEnumerable<XElement> tracks)
    {
        int maxTrackId = 0;
        foreach (var track in tracks)
        {
            int trackId = GetIntAttribute(track, "id", 0);
            maxTrackId = Mathf.Max(maxTrackId, trackId);
        }

        while (timelineGrid.GetTrackCount() <= maxTrackId)
        {
            timelineGrid.CreateNewTrack();
            yield return new WaitForEndOfFrame();
        }

        var allClipData = new List<(XElement clip, int trackId, float startTime)>();
        foreach (var track in tracks)
        {
            int trackId = GetIntAttribute(track, "id", 0);
            var clips = track.Element("Clips")?.Elements("Clip");
            if (clips != null)
            {
                foreach (var clip in clips)
                {
                    float startTime = GetFloatAttribute(clip, "start", 0);
                    allClipData.Add((clip, trackId, startTime));
                }
            }
        }

        allClipData.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        foreach (var (clip, trackId, startTime) in allClipData)
        {
            yield return StartCoroutine(ImportClipToTrack(clip, trackId));
            yield return new WaitForSeconds(0.1f);
        }

        timelineGrid.FixContentLayers();
    }


    private IEnumerator ImportClipToTrack(XElement clipElement, int trackId)
    {
        string type = clipElement.Attribute("type")?.Value;
        float startTime = GetFloatAttribute(clipElement, "start", 0);

        Debug.Log($"=== IMPORTING {type} to TRACK {trackId} at time {startTime:F2} ===");

        Content addedContent = null;

        switch (type)
        {
            case "Video":
                string videoPath = clipElement.Attribute("path")?.Value;
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    Debug.Log($"Adding video: {videoPath} to track {trackId}");
                    timelineGrid.AddVideoClipToTrack(videoPath, trackId);
                    yield return new WaitForEndOfFrame();

                    var videoControllers = timelineGrid.GetVideoControllers();
                    if (videoControllers.Count > 0)
                    {
                        addedContent = videoControllers[videoControllers.Count - 1].getvc();
                        Debug.Log($"Video added. Total videos: {videoControllers.Count}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Video file not found: {videoPath}");
                    yield break;
                }
                break;

            case "Image":
                string imagePath = clipElement.Attribute("path")?.Value;
                float imageDuration = GetFloatAttribute(clipElement, "duration", 5f);

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    Debug.Log($"Adding image: {imagePath} to track {trackId} with duration {imageDuration:F2}");
                    timelineGrid.AddImageToTrack(imagePath, trackId);
                    yield return new WaitForEndOfFrame();
                    timelineGrid.SetLastImageDuration(imageDuration);

                    var imageControllers = timelineGrid.GetImageControllers();
                    if (imageControllers.Count > 0)
                    {
                        addedContent = imageControllers[imageControllers.Count - 1].getic();
                    }
                }
                else
                {
                    Debug.LogWarning($"Image file not found: {imagePath}");
                    yield break;
                }
                break;

            case "Audio":
                string audioPath = clipElement.Attribute("path")?.Value;
                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    Debug.Log($"Adding audio: {audioPath} to track {trackId}");
                    timelineGrid.AddAudioToTrack(audioPath, trackId);
                    yield return new WaitForEndOfFrame();

                    var audioControllers = timelineGrid.GetAudioControllers();
                    if (audioControllers.Count > 0)
                    {
                        addedContent = audioControllers[audioControllers.Count - 1].getac();
                    }
                }
                else
                {
                    Debug.LogWarning($"Audio file not found: {audioPath}");
                    yield break;
                }
                break;

            case "Model":
                string prefabName = clipElement.Attribute("prefabName")?.Value;
                string animationName = clipElement.Attribute("animation")?.Value;
                if (!string.IsNullOrEmpty(prefabName))
                {
                    GameObject prefab = GetModelPrefab(prefabName);
                    if (prefab != null)
                    {
                        float modelDuration = GetAnimationDuration(prefab, animationName);
                        Debug.Log($"Adding model: {prefabName} to track {trackId} with duration {modelDuration:F2}");
                        timelineGrid.AddModelToTrack(prefab, trackId, modelDuration);
                        yield return new WaitForEndOfFrame();

                        if (timelineGrid.modelControllerList.Count > 0)
                        {
                            addedContent = timelineGrid.modelControllerList[timelineGrid.modelControllerList.Count - 1].getmc();
                            Debug.Log($"Model added. Total models: {timelineGrid.modelControllerList.Count}");
                            if (!string.IsNullOrEmpty(animationName))
                            {
                                SetLastModelAnimation(animationName);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model prefab not found: {prefabName}");
                        yield break;
                    }
                }
                break;
        }

        yield return new WaitForEndOfFrame();

        if (addedContent != null)
        {
            Debug.Log($"BEFORE positioning: Content layer = {addedContent.GetLayer()}");

            // Start time'ı ayarla
            timelineGrid.SetContentStartTime(addedContent, startTime);
            yield return new WaitForEndOfFrame();

            // Content'in hangi track'te olduğunu kontrol et
            int currentTrack = timelineGrid.RowIndexFor(addedContent);
            Debug.Log($"Content currently on track: {currentTrack}, should be on: {trackId}");

            // Eğer yanlış track'teyse, doğru track'e taşı
            if (currentTrack != trackId)
            {
                Debug.Log($"MOVING content from track {currentTrack} to track {trackId}");
                timelineGrid.MoveContentToTrack(addedContent, trackId);
                yield return new WaitForEndOfFrame();

                // Taşındıktan sonra tekrar kontrol et
                int finalTrack = timelineGrid.RowIndexFor(addedContent);
                Debug.Log($"AFTER MOVE: Content now on track: {finalTrack}");
            }

            var windowElement = clipElement.Element("Window");
            if (windowElement != null)
            {
                ApplyWindowData(addedContent, windowElement);
            }
        }
        else
        {
            Debug.LogWarning($"Could not find added content for {type}");
        }
    }

    private void ApplyWindowData(Content content, XElement windowElement)
    {
        float posX = GetFloatAttribute(windowElement, "posX", 0);
        float posY = GetFloatAttribute(windowElement, "posY", 0);
        float width = GetFloatAttribute(windowElement, "width", 100);
        float height = GetFloatAttribute(windowElement, "height", 100);

        posX = Mathf.Clamp(posX, -2000f, 2000f);
        posY = Mathf.Clamp(posY, -2000f, 2000f);
        width = Mathf.Clamp(width, 100f, 2000f);
        height = Mathf.Clamp(height, 100f, 2000f);

        Debug.Log($"Setting window properties: pos({posX:F1}, {posY:F1}), size({width:F1}, {height:F1})");

        timelineGrid.SetWindowProperties(content, posX, posY, width, height);

        float anchorMinX = Mathf.Clamp01(GetFloatAttribute(windowElement, "anchorMinX", 0.5f));
        float anchorMinY = Mathf.Clamp01(GetFloatAttribute(windowElement, "anchorMinY", 0.5f));
        float anchorMaxX = Mathf.Clamp01(GetFloatAttribute(windowElement, "anchorMaxX", 0.5f));
        float anchorMaxY = Mathf.Clamp01(GetFloatAttribute(windowElement, "anchorMaxY", 0.5f));
        float pivotX = Mathf.Clamp01(GetFloatAttribute(windowElement, "pivotX", 0.5f));
        float pivotY = Mathf.Clamp01(GetFloatAttribute(windowElement, "pivotY", 0.5f));

        timelineGrid.SetWindowAnchors(content,
            new Vector2(anchorMinX, anchorMinY),
            new Vector2(anchorMaxX, anchorMaxY),
            new Vector2(pivotX, pivotY));
    }


    // ==================== HELPER METHODS ====================

    private GameObject GetModelPrefab(string name)
    {
        return modelRegistry.FirstOrDefault(m => m.name == name)?.prefab;
    }

    private string GetModelName(GameObject prefab)
    {
        return modelRegistry.FirstOrDefault(m => m.prefab == prefab)?.name ?? prefab?.name ?? "Unknown";
    }

    private float GetAnimationDuration(GameObject modelPrefab, string animationName)
    {
        if (modelPrefab == null || string.IsNullOrEmpty(animationName)) return 10f;
        Animator animator = modelPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                {
                    return clip.length;
                }
            }
        }
        return 10f;
    }

    private void SetLastModelAnimation(string animationName)
    {
        var modelControllers = timelineGrid.GetContentManager().GetModelControllers();
        if (modelControllers.Count > 0)
        {
            var lastController = modelControllers[modelControllers.Count - 1];
            lastController.getmc()?.SetInitialAnimation(animationName);
        }
    }

    private float GetFloatAttribute(XElement element, string name, float defaultValue)
    {
        var attr = element.Attribute(name);
        if (attr == null) return defaultValue;

        string value = attr.Value.Replace(',', '.');

        bool success = float.TryParse(value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float result);

        Debug.Log($"Parsing {name}='{attr.Value}' -> '{value}' = {result:F3} (success: {success})");

        return success ? result : defaultValue;
    }

    private int GetIntAttribute(XElement element, string name, int defaultValue)
    {
        var attr = element.Attribute(name);
        if (attr == null) return defaultValue;

        string value = attr.Value.Replace(',', '.');

        return int.TryParse(value.Split('.')[0], out int result) ? result : defaultValue;
    }

    private bool GetBoolAttribute(XElement element, string name, bool defaultValue)
    {
        var attr = element.Attribute(name);
        return attr != null && bool.TryParse(attr.Value, out bool result) ? result : defaultValue;
    }
}