using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using TMPro;

public class TimelineGrid : MonoBehaviour
{
    [Header("Track Headers")]
    public RectTransform headersParent;
    public GameObject headerPrefab;

    [Header("Inspector Panel")]
    public InspectorPanelUI inspectorPanel;

    [Header("Prefabs & Parents")]
    public VideoPlayer VideoPrefab;
    public GameObject barPrefab;

    [Header("Configuration")]
    [SerializeField] private TimelineConfig config;

    [Tooltip("Your ScrollView Content (RectTransform)")]
    public RectTransform scrollViewContent;
    public RectTransform timeRulerParent;

    [Header("Per-clip Display Windows Root (overlay area for previews)")]
    public RectTransform playbackStackRoot;

    [Header("Timer")]
    public GameObject timerObject;
    private Timer timer;

    [Header("UI Prefabs")]
    public GameObject contextMenuPrefab;
    public GameObject eventMarkerPrefab;


    [Header("Time Ruler")]
    [SerializeField] private GameObject timeRulerPrefab;

    private string currentAddingContentType = "Track";

    [SerializeField] private ScrollRect scrollRectMain;


    // Managers
    private TimelineBarManager barManager;
    private TimelinePreviewManager previewManager;
    private TimelineWindowManager windowManager;
    private TimelineTrackManager trackManager;
    private TimelineSelectionManager selectionManager;
    private TimelineRulerManager rulerManager;
    private TimelineZoomManager zoomManager;
    private TimelineContentManager contentManager;
    private TimelinePlaybackManager playbackManager;

    private Canvas cachedCanvas;
    private bool flag = false;

    public readonly List<ModelController> modelControllerList = new();

    private static float savedResolutionWidth = 1920f;
    private static float savedResolutionHeight = 1080f;

    private void Awake()
    {
        if (inspectorPanel != null)
        {
            inspectorPanel.Initialize(this);
        }

        if (!config)
        {
            Debug.LogError("TimelineGrid: Config is not assigned!");
            return;
        }

        if (!scrollViewContent)
        {
            Debug.LogError("TimelineGrid: scrollViewContent is not assigned.");
            return;
        }

        FixContentAnchors();

        if (timerObject) timer = timerObject.GetComponent<Timer>();
        cachedCanvas = GetComponentInParent<Canvas>();

        InitializeManagers();

        trackManager.EnsureTracksRoot(scrollViewContent, config);
        UpdateContentHeight();

        selectionManager.EnsureSelectionOverlay(scrollViewContent);
        selectionManager.EnsureMarquee();

        foreach (var row in trackManager.GetTrackRows())
            AttachRowTriggers(row);

        //UpdateRowCloseButtons();
        rulerManager.CreateTimeRuler(scrollViewContent, config, timeRulerParent);
        rulerManager.UpdateTimeRuler(config);

        playbackManager = gameObject.AddComponent<TimelinePlaybackManager>();
        playbackManager.Initialize(timerObject);

        playbackManager.videoControllers = contentManager.GetVideoControllers();
        playbackManager.imageControllers = contentManager.GetImageControllers();
        playbackManager.audioControllers = contentManager.GetAudioControllers();
        playbackManager.modelControllers = modelControllerList;
    }

    private void InitializeManagers()
    {
        barManager = gameObject.AddComponent<TimelineBarManager>();
        barManager.Initialize(this);

        previewManager = gameObject.AddComponent<TimelinePreviewManager>();
        previewManager.Initialize(this);

        windowManager = gameObject.AddComponent<TimelineWindowManager>();
        windowManager.Initialize(this, playbackStackRoot);

        trackManager = gameObject.AddComponent<TimelineTrackManager>();
        trackManager.Initialize(this);

        selectionManager = gameObject.AddComponent<TimelineSelectionManager>();
        selectionManager.Initialize(this);

        rulerManager = gameObject.AddComponent<TimelineRulerManager>();
        rulerManager.Initialize(this);

        zoomManager = gameObject.AddComponent<TimelineZoomManager>();
        zoomManager.Initialize(this, cachedCanvas);

        contentManager = gameObject.AddComponent<TimelineContentManager>();
        contentManager.Initialize(this);
    }

    private void LateUpdate() => windowManager.UpdateWindowOrders();

    private void OnRectTransformDimensionsChange()
    {
        if (windowManager != null)
        {
            windowManager.OnRectTransformDimensionsChange();
        }
    }

    void Update()
    {
        zoomManager.HandleZoomInput();
        if (Input.GetMouseButtonDown(0))
        {
            CheckUIClick();
        }
    }

    void CheckUIClick()
    {
        if (EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            Debug.Log($"Clicked UI: {result.gameObject.name}");
            break;
        }
    }

    // Public controls
    public void playVideo()
    {
        Debug.Log("TimelineGrid: playVideo called.");
        flag = true;
        playbackManager.Play();
    }

    public void stopVideo()
    {
        Debug.Log("TimelineGrid: stopVideo called.");
        flag = false;
        playbackManager.Stop();
    }

    public void SkipForward15() => StepSeconds(15f);
    public void SkipBack15() => StepSeconds(-15f);
    public void StepSeconds(float delta)
    {
        float t = Mathf.Max(0f, GetTime() + delta);
        SetTime(t);
    }

    public bool getFlag() => flag;
    public float TimeToX(float t) => (t / 4f) * config.gridCellHorizontalPixelCount;
    public float XToTime(float x) => (x / (float)config.gridCellHorizontalPixelCount) * 4f;

    public void SetTime(float t)
    {
        playbackManager.SetTime(t);
    }

    public float GetTime()
    {
        return playbackManager != null ? playbackManager.GetTime() : 0f;
    }

    public void ShowOnSurface(VideoContent vc) => ShowContent(vc, vc.GetTexture());
    public void ShowOnSurfaceImage(ImageContent ic) => ShowContent(ic, ic.GetTexture());
    public void ShowOnSurface(ModelContent mc) { }

    // Content methods
    public Content addVideoClip(string contentPath) => contentManager.AddVideoClip(contentPath, VideoPrefab, barPrefab);
    public Content addImage(string contentPath) => contentManager.AddImage(contentPath, barPrefab);
    public Content addAudio(string contentPath) => contentManager.AddAudio(contentPath, barPrefab);

    public ModelContent addModelToTimeline(GameObject modelPrefab, float duration = 30f)
    {
        trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, headerPrefab, headersParent);
        int row = trackManager.GetAvailableTrackIndex();

        float videoEnd = (GetVideoControllers().Count > 0) ? GetVideoControllers()[^1].getvc().getEnd() : 0f;
        float imageEnd = (GetImageControllers().Count > 0) ? GetImageControllers()[^1].getic().getEnd() : 0f;
        float audioEnd = (GetAudioControllers().Count > 0) ? GetAudioControllers()[^1].getac().getEnd() : 0f;
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
        host.transform.SetParent(this.transform, false);

        var controller = ModelController.Create(host, timer, true, modelContent, this);
        modelControllerList.Add(controller);

        barManager.RegisterBar(modelContent.BarRT, modelContent, false);
        EnsureWidthForTime(newModelStart + duration);
        //UpdateRowCloseButtons();

        return modelContent;
    }

    public void ApplyAudioPreview(AudioContent audioContent) => contentManager.ApplyAudioPreview(audioContent);

    // Window methods
    public void ShowContent(Content c, Texture tex) => windowManager.ShowContent(c, tex);
    public void HideContent(Content c) => windowManager.HideContent(c);
    public void UpdateWindowOrders() => windowManager.UpdateWindowOrders();

    // Bar methods
    public void CreateBarPreviews(RectTransform barRT, Content content) =>
        previewManager.CreateBarPreviews(barRT, content);

    // Track methods
    public void EnsureRowsAtLeast(int n) =>
        trackManager.EnsureRowsAtLeast(n, config, headerPrefab, headersParent);
    public int GetRowIndexOf(RectTransform row) => trackManager.GetRowIndexOf(row);
    //public void UpdateRowCloseButtons() => trackManager.UpdateRowCloseButtons();
    public void UpdateTimeRuler() => rulerManager.UpdateTimeRuler(config);

    // Helpers
    public void EnsureWidthForTime(float endTimeSeconds)
    {
        float needRightPx = TimeToX(endTimeSeconds) + config.contentRightMarginPx;
        float curWidthPx = GetContentWidth();
        if (needRightPx > curWidthPx)
        {
            SetContentWidth(needRightPx);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
            var tracksRoot = trackManager.GetTracksRoot();
            if (tracksRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
            foreach (var row in trackManager.GetTrackRows())
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, needRightPx);
        }
    }

    public void EnsureContentWidthForBar(RectTransform barRT)
    {
        if (!barRT) return;
        float requiredRight = barRT.anchoredPosition.x + barRT.rect.width + config.contentRightMarginPx;
        if (requiredRight > GetContentWidth())
        {
            SetContentWidth(requiredRight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
            var tracksRoot = trackManager.GetTracksRoot();
            if (tracksRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
        }
    }

    public float GetContentWidth() => scrollViewContent ? scrollViewContent.rect.width : 0f;

    private void SetContentWidth(float width)
    {
        if (!scrollViewContent) return;
        scrollViewContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        foreach (var row in trackManager.GetTrackRows())
            row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    public void UpdateContentHeight()
    {
        if (!scrollViewContent) return;
        var trackRows = trackManager.GetTrackRows();
        int n = Mathf.Max(1, trackRows.Count);
        float rowsH = (n * config.laneHeight) + ((n - 1) * config.laneSpacing);
        float target = rowsH + config.verticalMargin + config.rulerHeight;

        scrollViewContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
    }

    public int LayerFromRow(int rowIndex) => rowIndex;

    public int RowIndexFor(Content c)
    {
        if (c != null && barManager.GetContentToBar().TryGetValue(c, out var bar) && bar && bar.parent is RectTransform row)
            return GetRowIndexOf(row);
        return int.MaxValue;
    }

    public void AttachRowTriggers(RectTransform row)
    {
        var trig = row.gameObject.GetComponent<EventTrigger>() ?? row.gameObject.AddComponent<EventTrigger>();
        AddEntry(trig, EventTriggerType.PointerDown, selectionManager.OnBackgroundPointerDown);
        AddEntry(trig, EventTriggerType.Drag, selectionManager.OnBackgroundDrag);
        AddEntry(trig, EventTriggerType.PointerUp, selectionManager.OnBackgroundPointerUp);
    }

    private void AddEntry(EventTrigger trig, EventTriggerType t, System.Action<BaseEventData> cb)
    {
        var e = new EventTrigger.Entry { eventID = t };
        e.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
        trig.triggers.Add(e);
    }

    // Snapping and overlap
    public float ComputeSnappedTime(float t, Content exclude)
    {
        float gridSecs = 4f;
        float tGrid = Mathf.Round(t / gridSecs) * gridSecs;

        var candidates = GetSnapTimes(exclude);
        float nearest = tGrid;
        float bestDist = Mathf.Abs(nearest - t);

        foreach (var st in candidates)
        {
            float d = Mathf.Abs(st - t);
            if (d < bestDist) { bestDist = d; nearest = st; }
        }

        float thrTime = XToTime(config.snapPixelThreshold);
        return (bestDist <= thrTime) ? Mathf.Max(0f, nearest) : Mathf.Max(0f, t);
    }

    public List<float> GetSnapTimes(Content exclude = null)
    {
        var list = new List<float> { 0f };
        foreach (var controller in contentManager.GetVideoControllers())
        {
            var c = controller.getvc();
            if (c == null || c == exclude) continue;
            list.Add(c.getStart());
            list.Add(c.getEnd());
        }

        foreach (var controller in contentManager.GetImageControllers())
        {
            var c = controller.getic();
            if (c == null || c == exclude) continue;
            float len = contentManager.GetLengthOverride(c, c.getLength());
            list.Add(c.getStart());
            list.Add(c.getStart() + len);
        }

        foreach (var controller in contentManager.GetAudioControllers())
        {
            var c = controller.getac();
            if (c == null || c == exclude) continue;
            list.Add(c.getStart());
            list.Add(c.getEnd());
        }

        foreach (var controller in modelControllerList)
        {
            var c = controller.getmc();
            if (c == null || c == exclude) continue;
            list.Add(c.getStart());
            list.Add(c.getEnd());
        }
        return list;
    }

    public float GetContentDuration(Content c)
    {
        if (c is ImageContent ic) return contentManager.GetLengthOverride(ic, ic.getLength());
        return Mathf.Max(0f, c.getEnd() - c.getStart());
    }

    public float ResolveNoOverlapStart(RectTransform row, Content moving, float desiredStart, float duration)
    {
        var intervals = new List<(float s, float e)>();
        foreach (var kv in barManager.GetContentToBar())
        {
            if (!kv.Value || kv.Value.parent != row || kv.Key == moving) continue;
            float s = kv.Key.getStart();
            float d = GetContentDuration(kv.Key);
            intervals.Add((s, s + d));
        }
        intervals.Sort((a, b) => a.s.CompareTo(b.s));

        if (Fits(intervals, desiredStart, desiredStart + duration))
            return Mathf.Max(0f, desiredStart);

        for (int i = 0; i <= intervals.Count; i++)
        {
            float gapStart = (i == 0) ? 0f : intervals[i - 1].e;
            float gapEnd = (i == intervals.Count) ? float.PositiveInfinity : intervals[i].s;

            float candidate = Mathf.Max(desiredStart, gapStart);
            if (candidate + duration <= gapEnd)
                return candidate;
        }
        return intervals.Count > 0 ? intervals[^1].e : Mathf.Max(0f, desiredStart);
    }

    private bool Fits(List<(float s, float e)> ints, float s, float e)
    {
        for (int i = 0; i < ints.Count; i++)
        {
            if (e <= ints[i].s) return true;
            if (s < ints[i].e && e > ints[i].s) return false;
        }
        return true;
    }

    public void MaybeMoveBarToRowUnderPointer(Vector2 screenPos, RectTransform draggingBar)
    {
        if (barManager.GetSelectedBars().Count > 1) return; // group stays in its rows
        var trackRows = trackManager.GetTrackRows();
        if (trackRows.Count == 0) return;

        var cam = cachedCanvas ? cachedCanvas.worldCamera : null;

        var row = trackManager.GetRowUnderPointer(screenPos, cachedCanvas);
        if (row != null)
        {
            if (draggingBar.parent != row)
            {
                draggingBar.SetParent(row, false);
                draggingBar.localScale = Vector3.one;
                draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
            }
            return;
        }

        bool canCreateMoreEmpty = trackManager.CountEmptyRows() == 0;

        var topRow = trackRows[0];
        var topCorners = new Vector3[4];
        topRow.GetWorldCorners(topCorners);
        float topEdgeY = RectTransformUtility.WorldToScreenPoint(cam, topCorners[1]).y;
        if (screenPos.y > topEdgeY + config.laneHeight * 0.25f)
        {
            if (canCreateMoreEmpty)
            {
                var newTop = trackManager.InsertRowAt(0, config, headerPrefab, headersParent);
                draggingBar.SetParent(newTop, false);
            }
            else
            {
                draggingBar.SetParent(topRow, false);
            }
            draggingBar.localScale = Vector3.one;
            draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
            return;
        }

        var bottomRow = trackRows[^1];
        var bottomCorners = new Vector3[4];
        bottomRow.GetWorldCorners(bottomCorners);
        float bottomEdgeY = RectTransformUtility.WorldToScreenPoint(cam, bottomCorners[0]).y;

        if (screenPos.y < bottomEdgeY - config.laneHeight * 0.25f)
        {
            if (canCreateMoreEmpty)
            {
                trackManager.EnsureRowsAtLeast(trackRows.Count + 1, config, headerPrefab, headersParent);
                var newBottom = trackRows[^1];
                draggingBar.SetParent(newBottom, false);
            }
            else
            {
                draggingBar.SetParent(bottomRow, false);
            }
            draggingBar.localScale = Vector3.one;
            draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
        }
    }

    private void FixContentAnchors()
    {
        var rt = scrollViewContent;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        if (rt.rect.width <= 0f)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800f);
    }

    public void ScreenToLocal(RectTransform target, Vector2 screen, out Vector2 local)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target, screen, cachedCanvas ? cachedCanvas.worldCamera : null, out local);
    }

    // XML Support Methods
    public void ClearAll()
    {
        stopVideo();
        contentManager.ClearAll();

        foreach (var kvp in barManager.GetContentToBar())
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);

        barManager.GetBarMap().Clear();
        barManager.GetContentToBar().Clear();
        windowManager.ClearWindows();

        trackManager.ClearTracks();

        if (rulerManager != null)
        {
            rulerManager.ClearEventMarkers();
        }

        UpdateContentHeight();
        //UpdateRowCloseButtons();
    }

    public Content GetLastAddedContent() => contentManager.GetLastAddedContent();

    public void SetContentStartTime(Content content, float startTime)
    {
        if (content == null) return;

        content.SetStart(startTime);

        if (barManager.GetContentToBar().TryGetValue(content, out var bar))
        {
            bar.anchoredPosition = new Vector2(TimeToX(startTime), 0);
        }
    }

    //public string GetCurrentTrackType()
    //{
    //    Content lastContent = GetLastAddedContent();

    //    if (lastContent != null)
    //    {
    //        return GetContentTypeName(lastContent);
    //    }

    //    return GetTrackTypeFromContext();
    //}

    //public string GetContentTypeName(Content content)
    //{
    //    if (content == null) return "Unknown";

    //    if (content is VideoContent) return "Video";
    //    else if (content is AudioContent) return "Audio";
    //    else if (content is ImageContent) return "Image";
    //    else if (content is ModelContent) return "Model";
    //    else return "Unknown";
    //}

    private string GetTrackTypeFromContext()
    {
        var stackTrace = new System.Diagnostics.StackTrace();

        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var methodName = frame.GetMethod().Name;

            if (methodName.Contains("addVideo") || methodName.Contains("AddVideo"))
                return "Video";
            else if (methodName.Contains("addImage") || methodName.Contains("AddImage"))
                return "Image";
            else if (methodName.Contains("addAudio") || methodName.Contains("AddAudio"))
                return "Audio";
            else if (methodName.Contains("addModel") || methodName.Contains("AddModel"))
                return "Model";
        }

        return "Track";
    }

    public void SetLastImageDuration(float duration) => contentManager.SetLastImageDuration(duration);
    public void SetImageLength(Content content, float duration) => contentManager.SetImageLength(content, duration);

    // Track Management
    public int GetTrackCount() => trackManager.GetTrackRows().Count;
    public void CreateNewTrack() => trackManager.EnsureRowsAtLeast(trackManager.GetTrackRows().Count + 1, config, headerPrefab, headersParent);

    public void MoveContentToTrack(Content content, int targetTrackIndex)
    {
        if (content == null) return;

        trackManager.EnsureRowsAtLeast(targetTrackIndex + 1, config, headerPrefab, headersParent);
        var trackRows = trackManager.GetTrackRows();

        if (targetTrackIndex >= trackRows.Count) return;

        if (!barManager.GetContentToBar().TryGetValue(content, out var bar)) return;

        RectTransform targetRow = trackRows[targetTrackIndex];

        bar.SetParent(targetRow, false);
        bar.localScale = Vector3.one;
        bar.anchoredPosition = new Vector2(bar.anchoredPosition.x, 0f);

        content.SetLayer(targetTrackIndex);

        UpdateWindowOrders();
        //UpdateRowCloseButtons();
    }

    // Window Management
    public void SetWindowProperties(Content content, float posX, float posY, float width, float height) =>
        windowManager.SetWindowProperties(content, posX, posY, width, height);

    public void SetWindowAnchors(Content content, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) =>
        windowManager.SetWindowAnchors(content, anchorMin, anchorMax, pivot);

    public Vector2 GetWindowPosition(Content content) => windowManager.GetWindowPosition(content);
    public Vector2 GetWindowSize(Content content) => windowManager.GetWindowSize(content);

    public void FixContentLayers()
    {
        foreach (var kvp in barManager.GetContentToBar())
        {
            var content = kvp.Key;
            var bar = kvp.Value;

            if (bar != null && bar.parent != null)
            {
                var trackRows = trackManager.GetTrackRows();
                for (int i = 0; i < trackRows.Count; i++)
                {
                    if (bar.parent == trackRows[i])
                    {
                        content.SetLayer(i);
                        break;
                    }
                }
            }
        }

        UpdateWindowOrders();
    }

    // Resolution Scaling
    public void SetSavedResolution(float width, float height)
    {
        savedResolutionWidth = width;
        savedResolutionHeight = height;
    }

    public Vector2 GetResolutionScale()
    {
        float currentWidth = Screen.width;
        float currentHeight = Screen.height;

        return new Vector2(
            currentWidth / savedResolutionWidth,
            currentHeight / savedResolutionHeight
        );
    }

    public void ApplyResolutionScaling()
    {
        Vector2 scale = GetResolutionScale();

        foreach (var kvp in windowManager.GetWindows())
        {
            var win = kvp.Value;
            if (win != null)
            {
                var winRoot = kvp.Value.GetType().GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(kvp.Value) as RectTransform;

                if (winRoot != null)
                {
                    Vector2 currentPos = winRoot.anchoredPosition;
                    Vector2 currentSize = winRoot.rect.size;

                    winRoot.anchoredPosition = new Vector2(
                        currentPos.x * scale.x,
                        currentPos.y * scale.y
                    );

                    winRoot.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal,
                        currentSize.x * scale.x
                    );

                    winRoot.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        currentSize.y * scale.y
                    );
                }
            }
        }
    }

    public ScrollRect GetScrollRect()
    {
        return scrollRectMain;
    }


    public void SetTrackVisibility(int trackIndex, bool isVisible) =>
        trackManager.SetTrackVisibility(trackIndex, isVisible);

    public void SetTrackLock(int trackIndex, bool isLocked) =>
        trackManager.SetTrackLock(trackIndex, isLocked);

    // Getters for managers
    public TimelineBarManager GetBarManager() => barManager;
    public TimelinePreviewManager GetPreviewManager() => previewManager;
    public TimelineWindowManager GetWindowManager() => windowManager;
    public TimelineTrackManager GetTrackManager() => trackManager;
    public TimelineSelectionManager GetSelectionManager() => selectionManager;
    public TimelineRulerManager GetRulerManager() => rulerManager;
    public TimelineZoomManager GetZoomManager() => zoomManager;
    public TimelineContentManager GetContentManager() => contentManager;

    // Getters for other components
    public TimelineConfig GetConfig() => config;
    public Canvas GetCanvas() => cachedCanvas;
    public float GetCanvasScale() => cachedCanvas ? cachedCanvas.scaleFactor : 1f;
    public Timer GetTimer() => timer;
    public InspectorPanelUI GetInspectorPanel() => inspectorPanel;
    public bool IsSelecting() => selectionManager.IsSelecting();

    // Getters for lists
    public List<VideoController> GetVideoControllers() => contentManager.GetVideoControllers();
    public List<ImageController> GetImageControllers() => contentManager.GetImageControllers();
    public List<AudioController> GetAudioControllers() => contentManager.GetAudioControllers();
    public List<RectTransform> GetTrackRows() => trackManager.GetTrackRows();
    public List<bool> GetTrackLockedStates() => trackManager.GetTrackLockedStates();
    public List<TrackHeader> GetTrackHeaders() => trackManager.GetTrackHeaders();

    public float GetLengthOverride(Content c, float fallback)
    {
        return contentManager.GetLengthOverride(c, fallback);
    }

    public void AddVideoClipToTrack(string contentPath, int trackIndex)
    {
        var contentManager = GetContentManager();
        contentManager.AddVideoClipToSpecificTrack(contentPath, VideoPrefab, barPrefab, trackIndex);
    }

    public void AddImageToTrack(string contentPath, int trackIndex)
    {
        var contentManager = GetContentManager();
        contentManager.AddImageToSpecificTrack(contentPath, barPrefab, trackIndex);
    }

    public void AddAudioToTrack(string contentPath, int trackIndex)
    {
        var contentManager = GetContentManager();
        contentManager.AddAudioToSpecificTrack(contentPath, barPrefab, trackIndex);
    }

    public void AddModelToTrack(GameObject modelPrefab, int trackIndex, float duration = 30f)
    {
        var contentManager = GetContentManager();
        contentManager.AddModelToSpecificTrack(modelPrefab, barPrefab, contextMenuPrefab, trackIndex, duration);
    }

    public void DeleteTrack(int trackIndex)
    {
        var trackRows = GetTrackRows();
        var trackHeaders = GetTrackHeaders();
        var trackLockedStates = GetTrackLockedStates();

        if (trackIndex < 0 || trackIndex >= trackRows.Count) return;

        var barManager = GetBarManager();
        foreach (var kvp in barManager.GetContentToBar())
        {
            if (kvp.Value != null && kvp.Value.parent == trackRows[trackIndex])
            {
                Debug.LogWarning($"Cannot delete track {trackIndex}: contains content");
                return;
            }
        }

        if (trackRows[trackIndex] != null)
            Destroy(trackRows[trackIndex].gameObject);

        if (trackIndex < trackHeaders.Count && trackHeaders[trackIndex] != null)
            Destroy(trackHeaders[trackIndex].gameObject);

        trackRows.RemoveAt(trackIndex);
        trackHeaders.RemoveAt(trackIndex);
        trackLockedStates.RemoveAt(trackIndex);

        for (int i = trackIndex; i < trackHeaders.Count; i++)
        {
            if (trackHeaders[i] != null)
            {
                trackHeaders[i].UpdateTrackIndex(i);
            }
        }

        FixContentLayers();
        UpdateContentHeight();

        Debug.Log($"Track {trackIndex} deleted successfully");
    }

    //NOTE: Refactoring this was A LOT of work! Please be careful when modifying. Thanks :) Egemen.
}