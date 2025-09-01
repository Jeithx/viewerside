//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using UnityEngine.Video;


//public class TimelineGridGC : MonoBehaviour
//{

//    [Header("Track Headers")]
//    public RectTransform headersParent;
//    public GameObject headerPrefab;
//    private readonly List<bool> trackLockedStates = new List<bool>();
//    private readonly List<TrackHeader> trackHeaders = new List<TrackHeader>();

//    [Header("Inspector Panel")]
//    public InspectorPanelUI inspectorPanel;



//    [Header("Prefabs & Parents")]
//    public VideoPlayer VideoPrefab;
//    public GameObject barPrefab;


//    [Header("Configuration")]
//    [SerializeField] private TimelineConfig config;

//    [Tooltip("Your ScrollView Content (RectTransform)")]
//    public RectTransform scrollViewContent;

//    [Tooltip("Auto-created under Content as 'TracksRoot' if left empty.")]
//    public RectTransform tracksRoot;

//    [Header("Per-clip Display Windows Root (overlay area for previews)")]
//    public RectTransform playbackStackRoot;

//    [Header("Timer")]
//    public GameObject timerObject;
//    private Timer timer;


//    [Header("UI Prefabs")]
//    public GameObject contextMenuPrefab; // From HEAD branch



//    private ScrollRect parentScrollRect;
//    private float lastMouseScrollTime;

//    [Header("Time Ruler")]
//    [SerializeField] private GameObject timeRulerPrefab;


//    private RectTransform timeRulerRT;
//    private List<GameObject> timeMarkers = new List<GameObject>();
//    private Font timeFont;

//    // play state
//    private bool flag = false;

//    // Controllers
//    private readonly List<VideoController> videoControllerList = new();
//    private readonly List<ImageController> imageControllerList = new();
//    private readonly List<AudioController> audioControllerList = new();
//    public readonly List<ModelController> modelControllerList = new(); // From HEAD branch

//    // Bars & rows
//    private readonly Dictionary<RectTransform, Content> barMap = new();
//    private readonly Dictionary<Content, RectTransform> contentToBar = new();
//    private readonly List<RectTransform> trackRows = new(); // row 0 = top

//    // Per-clip windows
//    private class Win { public RectTransform root; public RawImage view; public RawImageFitter fitter; }
//    private readonly Dictionary<Content, Win> windows = new();

//    // Overridden image lengths (due to bar resize)
//    private readonly Dictionary<Content, float> imageLengths = new();

//    private readonly Dictionary<Content, Coroutine> activeFrameCoroutines = new();
//    public float GetLengthOverride(Content c, float fallback) =>
//        imageLengths.TryGetValue(c, out var v) ? v : fallback;

//    // drag/resize state
//    private RectTransform draggingBar;
//    private RectTransform resizingBar;
//    private bool resizingLeft;
//    private float barGrabOffsetX; // for smooth follow

//    // group selection
//    private readonly HashSet<RectTransform> selectedBars = new();
//    private readonly Dictionary<RectTransform, Vector2> groupStartPos = new();
//    private bool draggingGroup;


//    private TimelinePlaybackManager playbackManager;



//    // marquee selection (overlay above rows)
//    private RectTransform selectionOverlayRT; // parent container above tracks
//    private RectTransform marqueeRT;
//    private bool selecting;
//    private Vector2 selectStartScreen;
//    private bool additiveSelect; // Ctrl/Command additive selection

//    private Canvas cachedCanvas;

//    private static Dictionary<string, Texture2D[]> videoFrameCache = new Dictionary<string, Texture2D[]>();


//    // ---------- Unity ----------
//    private void Awake()
//    {
//        if (inspectorPanel != null)
//        {
//            inspectorPanel.Initialize(this);
//        }

//        if (!config)
//        {
//            Debug.LogError("TimelineGrid: Config is not assigned!");
//            return;
//        }

//        if (!scrollViewContent)
//        {
//            Debug.LogError("TimelineGrid: scrollViewContent is not assigned.");
//            return;
//        }

//        FixContentAnchors(); // make content left-anchored

//        if (timerObject) timer = timerObject.GetComponent<Timer>();
//        cachedCanvas = GetComponentInParent<Canvas>();

//        EnsureTracksRoot();
//        //EnsureRowsAtLeast(1);
//        UpdateContentHeight();

//        EnsureSelectionOverlay(); // overlay that draws on top
//        EnsureMarquee();           // the semi-transparent rectangle

//        // Attach background triggers PER ROW (so empty-space drags start selection)
//        foreach (var row in trackRows) AttachRowTriggers(row);

//        parentScrollRect = GetComponentInParent<ScrollRect>();

//        UpdateRowCloseButtons();
//        CreateTimeRuler();
//        UpdateTimeRuler();

//        // Initialize PlaybackManager
//        playbackManager = gameObject.AddComponent<TimelinePlaybackManager>();
//        playbackManager.Initialize(timerObject);

//        playbackManager.videoControllers = videoControllerList;
//        playbackManager.imageControllers = imageControllerList;
//        playbackManager.audioControllers = audioControllerList;
//        playbackManager.modelControllers = modelControllerList;
//    }

//    private void LateUpdate() => UpdateWindowOrders();

//    private void OnRectTransformDimensionsChange()
//    {
//        foreach (var kv in windows)
//        {
//            var w = kv.Value;
//            if (w.root != null && w.root.gameObject.activeInHierarchy)
//                w.fitter?.FitNow(w.view ? w.view.texture : null);
//        }
//    }

//    // ---------- Public controls ----------
//    public void playVideo()
//    {
//        flag = true;
//        playbackManager.Play();
//    }

//    public void stopVideo()
//    {
//        flag = false;
//        playbackManager.Stop();
//    }

//    // Skip buttons
//    public void SkipForward15() => StepSeconds(15f);
//    public void SkipBack15() => StepSeconds(-15f);
//    public void StepSeconds(float delta)
//    {
//        float t = Mathf.Max(0f, GetTime() + delta);
//        SetTime(t);
//    }

//    public bool getFlag() => flag;
//    public float TimeToX(float t) => (t / 4f) * config.gridCellHorizontalPixelCount;

//    public float XToTime(float x) => (x / (float)config.gridCellHorizontalPixelCount) * 4f;

//    public void SetTime(float t)
//    {
//        playbackManager.SetTime(t);
//    }

//    public float GetTime()
//    {
//        return playbackManager != null ? playbackManager.GetTime() : 0f;
//    }
//    public void ShowOnSurface(VideoContent vc) => ShowContent(vc, vc.GetTexture());
//    public void ShowOnSurfaceImage(ImageContent ic) => ShowContent(ic, ic.GetTexture());

//    public void ShowOnSurface(ModelContent mc)
//    {
//        // Models will be rendered in 3D space, overlaying the video
//        // The 3D model will automatically appear in front of the 2D video surface
//        // No additional code needed here for now
//    }

//    // ---------- Add clips ----------
//    public void addVideoClip(string contentPath)
//    {
//        EnsureRowsAtLeast(trackRows.Count + 1);
//        int row = GetAvailableTrackIndex();

//        float newStart = Mathf.Max(
//            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
//            Mathf.Max(
//                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
//                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

//        var clip = new VideoContent(VideoPrefab, barPrefab, contentPath,
//            config.gridCellHorizontalPixelCount, trackRows[row], newStart, this);

//        var host = new GameObject($"VideoController_{videoControllerList.Count}");
//        host.transform.SetParent(this.transform, false);
//        var controller = VideoController.Create(host, timer, clip, this);
//        videoControllerList.Add(controller);

//        EnsureWidthForTime(newStart + 0.1f);
//        ShowContent(clip, clip.GetTexture());
//        HideContent(clip);
//        StartCoroutine(RegisterBarWhenReady(clip));
//        UpdateRowCloseButtons();
//    }

//    private int GetAvailableTrackIndex()
//    {
//        // İlk boş track'i bul
//        for (int i = 0; i < trackRows.Count; i++)
//        {
//            if (!RowHasBars(trackRows[i]))
//                return i;
//        }

//        // Boş track yoksa yeni oluştur
//        EnsureRowsAtLeast(trackRows.Count + 1);
//        return trackRows.Count - 1;
//    }

//    public void addImage(string contentPath)
//    {
//        EnsureRowsAtLeast(trackRows.Count + 1);
//        int row = trackRows.Count - 1;

//        float newStart = Mathf.Max(
//            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
//            Mathf.Max(
//                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
//                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

//        var clip = new ImageContent(barPrefab, contentPath,
//            config.gridCellHorizontalPixelCount, trackRows[row], newStart);

//        var host = new GameObject($"ImageController_{imageControllerList.Count}");
//        host.transform.SetParent(this.transform, false);
//        var controller = ImageController.Create(host, timer, clip, this);
//        imageControllerList.Add(controller);

//        ShowContent(clip, clip.GetTexture());
//        HideContent(clip);
//        if (clip.BarRT) RegisterBar(clip.BarRT, clip, isImage: true);

//        EnsureWidthForTime(newStart + clip.getLength());
//        UpdateRowCloseButtons();
//    }

//    public void addAudio(string contentPath)
//    {
//        EnsureRowsAtLeast(trackRows.Count + 1);
//        int row = trackRows.Count - 1;

//        float newStart = Mathf.Max(
//            (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f,
//            Mathf.Max(
//                (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f,
//                (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f));

//        var clip = new AudioContent(barPrefab, contentPath,
//            config.gridCellHorizontalPixelCount, trackRows[row], newStart, this);

//        var host = new GameObject($"AudioController_{audioControllerList.Count}");
//        host.transform.SetParent(this.transform, false);
//        var controller = AudioController.Create(host, timer, clip, this, contentPath);
//        audioControllerList.Add(controller);

//        if (clip.BarRT) RegisterBar(clip.BarRT, clip, isImage: false);

//        EnsureWidthForTime(newStart + 0.1f);
//        UpdateRowCloseButtons();
//    }

//    // TimelineGrid.cs'in en altına bu İKİ YENİ FONKSİYONU ekleyin

//    // AudioController, ses yüklendiğinde bu fonksiyonu çağıracak
//    public void ApplyAudioPreview(AudioContent audioContent)
//    {
//        if (audioContent != null)
//        {
//            StartCoroutine(ApplyAudioPreviewRoutine(audioContent));
//        }
//    }

//    private IEnumerator ApplyAudioPreviewRoutine(AudioContent audioContent)
//    {
//        // AudioClip'in asset yolunu al
//        string path = audioContent.getPath();
//        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

//        if (clip == null) yield break;

//        Texture2D previewTexture = null;
//        int maxAttempts = 20; // En fazla 2 saniye bekle

//        // Unity önizlemeyi hazırlayana kadar birkaç deneme yap
//        for (int i = 0; i < maxAttempts; i++)
//        {
//            previewTexture = AssetPreview.GetAssetPreview(clip);
//            if (previewTexture != null)
//            {
//                break; // Önizleme hazır, döngüden çık
//            }
//            yield return new WaitForSeconds(0.1f);
//        }

//        // Eğer önizleme başarıyla alındıysa, bar'a uygula
//        if (previewTexture != null && audioContent.BarRT != null)
//        {
//            Image barImage = audioContent.BarRT.GetComponent<Image>();
//            if (barImage != null)
//            {
//                // Orijinal sarı rengi kullanmak yerine dokuyu direkt göster
//                barImage.color = Color.white;
//                barImage.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), new Vector2(0.5f, 0.5f));

//                // Dokunun bar'ı kaplaması için ayarlar
//                barImage.type = Image.Type.Sliced;
//            }
//        }
//        else
//        {
//            Debug.LogWarning("Ses dosyası için önizleme alınamadı: " + path);
//        }
//    }

//    // ----- Add models (from HEAD branch, adapted) -----
//    public void addModelToTimeline(GameObject modelPrefab, float duration = 30f)
//    {
//        EnsureRowsAtLeast(trackRows.Count + 1);
//        int row = trackRows.Count - 1;

//        // Correctly calculate the start time based on all existing content types
//        float videoEnd = (videoControllerList.Count > 0) ? videoControllerList[^1].getvc().getEnd() : 0f;
//        float imageEnd = (imageControllerList.Count > 0) ? imageControllerList[^1].getic().getEnd() : 0f;
//        float audioEnd = (audioControllerList.Count > 0) ? audioControllerList[^1].getac().getEnd() : 0f;
//        float modelEnd = (modelControllerList.Count > 0) ? modelControllerList[^1].getmc().getEnd() : 0f;
//        float newModelStart = Mathf.Max(videoEnd, imageEnd, audioEnd, modelEnd);

//        // Note: Assuming `ModelContent` uses `barPrefab` for its timeline representation.
//        // If it requires a different prefab, you might need to add it to the fields.
//        var modelContent = new ModelContent(
//            modelPrefab,
//            barPrefab,
//            config.gridCellHorizontalPixelCount,
//            contextMenuPrefab,
//            trackRows[row], // Add to the new row
//            newModelStart,
//            duration);

//        var host = new GameObject($"ModelController_{modelControllerList.Count}");
//        host.transform.SetParent(this.transform, false);

//        var controller = ModelController.Create(host, timer, true, modelContent, this);
//        modelControllerList.Add(controller);

//        // Register the bar for the model
//        if (modelContent.BarRT) RegisterBar(modelContent.BarRT, modelContent, isImage: false);

//        EnsureWidthForTime(newModelStart + duration);
//        UpdateRowCloseButtons();
//    }


//    private System.Collections.IEnumerator RegisterBarWhenReady(VideoContent clip)
//    {
//        while (clip == null || clip.BarRT == null) yield return null;

//        // Bar size hesaplanana kadar bekle
//        yield return new WaitForEndOfFrame();
//        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
//        yield return null;

//        RegisterBar(clip.BarRT, clip, isImage: false);
//        UpdateRowCloseButtons();
//    }

//    // ---------- Tracks (rows) ----------
//    private void EnsureTracksRoot()
//    {
//        if (!tracksRoot)
//        {
//            var go = new GameObject("TracksRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
//            tracksRoot = go.GetComponent<RectTransform>();
//            tracksRoot.SetParent(scrollViewContent, false);
//            tracksRoot.anchorMin = new Vector2(0, 1);
//            tracksRoot.anchorMax = new Vector2(0, 1);
//            tracksRoot.pivot = new Vector2(0, 1);
//            tracksRoot.anchoredPosition = Vector2.zero;
//            tracksRoot.sizeDelta = Vector2.zero;

//            var vlg = tracksRoot.GetComponent<VerticalLayoutGroup>();
//            vlg.spacing = config.laneSpacing;
//            vlg.padding = new RectOffset(0, 0, 38, 0);
//            vlg.childAlignment = TextAnchor.UpperLeft;
//            vlg.childForceExpandWidth = true;
//            vlg.childForceExpandHeight = false;
//            vlg.childControlWidth = true;
//            vlg.childControlHeight = false;
//            LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
//        }
//    }

//    public void EnsureWidthForTime(float endTimeSeconds)
//    {
//        float needRightPx = TimeToX(endTimeSeconds) + config.contentRightMarginPx;
//        float curWidthPx = GetContentWidth();
//        if (needRightPx > curWidthPx)
//        {
//            SetContentWidth(needRightPx);
//            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
//            if (tracksRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
//            for (int i = 0; i < trackRows.Count; i++)
//                trackRows[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, needRightPx);
//        }
//    }

//    private RectTransform CreateRow()
//    {
//        var go = new GameObject($"Row{trackRows.Count}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
//        var rt = go.GetComponent<RectTransform>();
//        rt.SetParent(tracksRoot, false);

//        rt.anchorMin = new Vector2(0, 0.5f);
//        rt.anchorMax = new Vector2(0, 0.5f);
//        rt.pivot = new Vector2(0, 0.5f);
//        rt.sizeDelta = new Vector2(GetContentWidth(), config.laneHeight);

//        var img = go.GetComponent<Image>(); img.color = config.laneColor; img.raycastTarget = true;

//        var le = go.GetComponent<LayoutElement>();
//        le.minHeight = le.preferredHeight = config.laneHeight;

//        AddCloseButtonToRow(rt);
//        AttachRowTriggers(rt); // important: rows receive background drags for marquee

//        trackRows.Add(rt);
//        trackLockedStates.Add(false);

//        if (headerPrefab != null && headersParent != null)
//        {
//            GameObject headerGO = Instantiate(headerPrefab, headersParent);
//            TrackHeader header = headerGO.GetComponent<TrackHeader>();
//            if (header != null)
//            {
//                header.Initialize(trackRows.Count - 1, this, $"Track {trackRows.Count}");
//                trackHeaders.Add(header);
//            }
//        }
//        UpdateContentHeight();
//        return rt;
//    }

//    private RectTransform InsertRowAt(int index)
//    {
//        var rt = CreateRow();
//        int sibling = Mathf.Clamp(index, 0, tracksRoot.childCount - 1);
//        rt.SetSiblingIndex(sibling);
//        trackRows.Insert(Mathf.Clamp(index, 0, trackRows.Count), rt);
//        UpdateContentHeight();
//        UpdateRowCloseButtons();
//        return rt;
//    }

//    private void EnsureRowsAtLeast(int n)
//    {
//        while (trackRows.Count < n) CreateRow();
//        UpdateRowCloseButtons();
//    }

//    private int GetRowIndexOf(RectTransform row)
//    {
//        for (int i = 0; i < trackRows.Count; i++) if (trackRows[i] == row) return i;
//        return -1;
//    }

//    private int LayerFromRow(int rowIndex) => rowIndex; // 0 (top row) draws on top

//    void Update()
//    {
//        HandleZoomInput();
//        if (Input.GetMouseButtonDown(0))
//        {
//            CheckUIClick();
//        }
//    }

//    void CheckUIClick()
//    {
//        if (EventSystem.current == null) return;

//        PointerEventData pointerData = new PointerEventData(EventSystem.current)
//        {
//            position = Input.mousePosition
//        };

//        List<RaycastResult> results = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(pointerData, results);

//        foreach (var result in results)
//        {
//            Debug.Log($"Clicked UI: {result.gameObject.name}");
//            // İlk bulduğunu kullan ve çık
//            break;
//        }
//    }

//    // ---------- Bars / drag / resize ----------
//    private void RegisterBar(RectTransform barRT, Content content, bool isImage)
//    {
//        if (!barRT || content == null) return;

//        if (!(barRT.parent is RectTransform parentRow) || GetRowIndexOf(parentRow) < 0)
//        {
//            EnsureRowsAtLeast(1);
//            barRT.SetParent(trackRows[^1], false);
//            barRT.localScale = Vector3.one;
//        }
//        barRT.anchoredPosition = new Vector2(barRT.anchoredPosition.x, 0f);

//        barMap[barRT] = content;
//        contentToBar[content] = barRT;

//        int rowIdx = GetRowIndexOf(barRT.parent as RectTransform);
//        content.SetLayer(LayerFromRow(rowIdx));
//        UpdateWindowOrders();

//        var img = barRT.GetComponent<Image>() ?? barRT.gameObject.AddComponent<Image>();
//        img.raycastTarget = true;

//        barRT.SetAsLastSibling();

//        var trig = barRT.gameObject.GetComponent<EventTrigger>() ?? barRT.gameObject.AddComponent<EventTrigger>();
//        AddEntry(trig, EventTriggerType.PointerDown, OnBarPointerDown);
//        AddEntry(trig, EventTriggerType.BeginDrag, OnBarBeginDrag);
//        AddEntry(trig, EventTriggerType.Drag, OnBarDrag);
//        AddEntry(trig, EventTriggerType.EndDrag, OnBarEndDrag);

//        if (isImage)
//        {
//            AddBarResizeHandle(barRT, content, true);
//            AddBarResizeHandle(barRT, content, false);
//        }

//        EnsureContentWidthForBar(barRT);
//        (barRT.parent as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetContentWidth());
//        UpdateRowCloseButtons();

//        if (config.enablePreviews)
//        {
//            CreateBarPreviews(barRT, content);
//        }


//        var currentParentRow = barRT.parent as RectTransform;
//        int rowIndex = GetRowIndexOf(currentParentRow);

//        if (rowIndex != -1 && rowIndex < trackHeaders.Count && currentParentRow.childCount <= 2)
//        {
//            string contentName = "Content";
//            if (content is VideoContent vc)
//            {
//                contentName = System.IO.Path.GetFileNameWithoutExtension(vc.getPath());
//            }
//            else if (content is ImageContent ic)
//            {
//                contentName = System.IO.Path.GetFileNameWithoutExtension(ic.getPath());
//            }
//            else if (content is AudioContent ac)
//            {
//                contentName = System.IO.Path.GetFileNameWithoutExtension(ac.getPath());
//            }
//            else if (content is ModelContent mc)
//            {
               
//                var modelPrefabField = typeof(ModelContent).GetField("_modelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//                if (modelPrefabField != null)
//                {
//                    GameObject prefab = modelPrefabField.GetValue(mc) as GameObject;
//                    if (prefab != null)
//                    {
//                        contentName = prefab.name;
//                    }
//                    else
//                    {
//                        contentName = "3D Model";
//                    }
//                }
//            }

//            trackHeaders[rowIndex].SetTrackName(contentName);
//        }

//    }

//    private void CreateBarPreviews(RectTransform barRT, Content content)
//    {
//        if( content is AudioContent)
//        {
//            return;
//        }

//        if (!barRT || content == null) return;

//        GameObject containerGO = new GameObject("PreviewContainer", typeof(RectTransform));
//        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
//        containerRT.SetParent(barRT, false);

//        containerRT.anchorMin = Vector2.zero;
//        containerRT.anchorMax = Vector2.one;
//        containerRT.offsetMin = Vector2.zero;
//        containerRT.offsetMax = Vector2.zero;

//        // Dinamik preview sayısı hesapla
//        int dynamicPreviewCount = GetDynamicPreviewCount(barRT);
//        float barWidth = barRT.rect.width;
//        float previewWidth = (barWidth - (config.previewPadding * (dynamicPreviewCount + 1))) / dynamicPreviewCount;

//        if (previewWidth > 5f)
//        {
//            RawImage[] previewImages = new RawImage[dynamicPreviewCount];  // ✅
//            for (int i = 0; i < dynamicPreviewCount; i++)  // ✅
//            {
//                CreateSinglePreview(containerRT, content, i, previewWidth, dynamicPreviewCount);  // ✅
//                previewImages[i] = containerRT.Find($"Preview_{i}").GetComponent<RawImage>();
//            }

//            if (content is VideoContent videoContent)
//            {
//                StartCoroutine(CaptureVideoFramesWithCache(videoContent, previewImages));
//            }
//        }
//    }

//    private System.Collections.IEnumerator CaptureVideoFramesWithCache(VideoContent videoContent, RawImage[] previewImages)
//    {
//        string videoPath = videoContent.getPath();
//        config.previewCount = previewImages.Length;
//        string cacheKey = $"{videoPath}_{config.previewCount}";

//        // 1. Adım: Cache'de var mı diye kontrol et
//        if (videoFrameCache.TryGetValue(cacheKey, out Texture2D[] cachedFrames))
//        {
//            for (int i = 0; i < previewImages.Length && i < cachedFrames.Length; i++)
//            {
//                // previewImage'ın hala var olup olmadığını kontrol et
//                if (previewImages[i] != null && cachedFrames[i] != null)
//                {
//                    previewImages[i].texture = cachedFrames[i];
//                }
//            }
//            yield break; // Cache'den yüklendi, metodu sonlandır.
//        }

//        // 2. Adım: Cache'de yoksa, yeni bir frame yakalama işlemi başlat
//        // Coroutine'i başlat, referansını sakla ve bitmesini bekle
//        Coroutine captureCoroutine = StartCoroutine(CaptureVideoFrames(videoContent, previewImages));
//        activeFrameCoroutines[videoContent] = captureCoroutine;

//        yield return captureCoroutine; // Coroutine'in bitmesini bekle

//        // 3. Adım: İşlem bittiğinde veya durdurulduğunda referansı temizle
//        // Dışarıdan durdurulmuş olabileceği için anahtarın hala var olup olmadığını kontrol et
//        if (activeFrameCoroutines.ContainsKey(videoContent))
//        {
//            activeFrameCoroutines.Remove(videoContent);
//        }

//        // 4. Adım: Yakalanan yeni kareleri cache'e kaydet
//        Texture2D[] framesToCache = new Texture2D[previewImages.Length];
//        bool anyFrameCaptured = false;
//        for (int i = 0; i < previewImages.Length; i++)
//        {
//            // Coroutine yarıda kesilmiş olabileceğinden, previewImages elemanlarının hala geçerli olup olmadığını kontrol et
//            if (previewImages[i] != null && previewImages[i].texture is Texture2D tex)
//            {
//                framesToCache[i] = tex;
//                anyFrameCaptured = true;
//            }
//        }

//        // Sadece en az bir kare başarıyla yakalandıysa cache'e ekle
//        if (anyFrameCaptured)
//        {
//            videoFrameCache[cacheKey] = framesToCache;
//        }
//    }

//    private System.Collections.IEnumerator CaptureVideoFrames(VideoContent videoContent, RawImage[] previewImages)
//    {
//        var videoPlayer = videoContent.GetVideoPlayer();
//        if (!videoPlayer) yield break;

//        videoPlayer.Prepare();
//        while (!videoPlayer.isPrepared)
//            yield return new WaitForSeconds(0.1f);

//        float videoDuration = videoContent.getLength();

//        yield return StartCoroutine(CaptureFramesAsync(videoPlayer, videoDuration, previewImages));

//        videoPlayer.Stop();
//        videoPlayer.time = 0;
//    }

//    private System.Collections.IEnumerator CaptureFramesAsync(VideoPlayer player, float duration, RawImage[] previewImages)
//    {
//        // Dinamik array boyutu kullan
//        for (int i = 0; i < previewImages.Length; i++) // config.previewCount yerine previewImages.Length
//        {
//            if (previewImages[i] == null) continue;

//            float frameTime = (duration / previewImages.Length) * i; // Burada da

//            player.time = frameTime;
//            player.Play();

//            yield return new WaitForSeconds(0.1f);

//            if (player.texture != null)
//            {
//                previewImages[i].texture = DuplicateTextureFast(player.texture);
//            }

//            player.Pause();
//            yield return null;
//        }
//    }

//    private int GetDynamicPreviewCount(RectTransform barRT)
//    {
//        if (!barRT) return 2;

//        float barWidth = barRT.rect.width;

//        // Zoom seviyesine göre base preview count hesapla
//        float zoomFactor = config.gridCellHorizontalPixelCount / 20f; // 20 = default zoom
//        int basePreviewCount = Mathf.RoundToInt(config.previewCount * Mathf.Sqrt(zoomFactor));

//        int maxPreviewsByWidth = Mathf.FloorToInt((barWidth - config.previewPadding) / (30f + config.previewPadding));

//        // Sonucu sınırla
//        int finalCount = Mathf.Clamp(
//            Mathf.Min(basePreviewCount, maxPreviewsByWidth),
//            1,  // minimum
//            20  // maximum
//        );

//        return finalCount;
//    }

//    private class FrameCaptureData
//    {
//        public float targetTime;
//        public RawImage targetImage;
//        public bool captured;
//    }

//    private Texture2D DuplicateTextureFast(Texture source)
//    {
//        int width = Mathf.Min(source.width, 128);
//        int height = Mathf.Min(source.height, 72);

//        RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default);

//        Graphics.Blit(source, tempRT);

//        RenderTexture.active = tempRT;
//        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
//        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        result.Apply();

//        RenderTexture.active = null;
//        RenderTexture.ReleaseTemporary(tempRT);

//        return result;
//    }

//    public static void ClearVideoFrameCache()
//    {
//        foreach (var frames in videoFrameCache.Values)
//        {
//            foreach (var frame in frames)
//            {
//                if (frame != null) Destroy(frame);
//            }
//        }
//        videoFrameCache.Clear();
//    }




//    //private Texture2D DuplicateTexture(Texture source)
//    //{
//    //    RenderTexture renderTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

//    //    Graphics.Blit(source, renderTex);

//    //    RenderTexture previous = RenderTexture.active;
//    //    RenderTexture.active = renderTex;

//    //    Texture2D readableText = new Texture2D(source.width, source.height);
//    //    readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
//    //    readableText.Apply();

//    //    RenderTexture.active = previous;
//    //    RenderTexture.ReleaseTemporary(renderTex);

//    //    return readableText;
//    //}

//    private void CreateSinglePreview(RectTransform container, Content content, int index, float previewWidth, int totalPreviews)
//    {
//        GameObject previewGO = new GameObject($"Preview_{index}", typeof(RectTransform), typeof(RawImage));
//        RectTransform previewRT = previewGO.GetComponent<RectTransform>();
//        RawImage previewImage = previewGO.GetComponent<RawImage>();

//        previewRT.SetParent(container, false);

//        float xPos = config.previewPadding + (index * (previewWidth + config.previewPadding));

//        previewRT.anchorMin = new Vector2(0, 0);
//        previewRT.anchorMax = new Vector2(0, 1);
//        previewRT.pivot = new Vector2(0, 0.5f);
//        previewRT.anchoredPosition = new Vector2(xPos, 0);
//        previewRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, previewWidth);
//        previewRT.offsetMin = new Vector2(previewRT.offsetMin.x, 2f);
//        previewRT.offsetMax = new Vector2(previewRT.offsetMax.x, -2f);

//        previewImage.raycastTarget = false;

//        if (content is ImageContent imageContent)
//        {
//            previewImage.texture = imageContent.GetTexture();
//        }
//        else if (content is ModelContent modelContent)
//        {
//            previewImage.color = new Color(0.3f, 0.3f, 0.8f, 0.5f);
//        }
//    }



//    // remake the bars when the content changes
//    private void RefreshBarPreviews(RectTransform barRT, Content content)
//    {
//        // YENİ EKLENEN BLOK: Eski coroutine'i durdur
//        if (activeFrameCoroutines.TryGetValue(content, out Coroutine runningCoroutine))
//        {
//            if (runningCoroutine != null)
//            {
//                StopCoroutine(runningCoroutine);
//            }
//            activeFrameCoroutines.Remove(content);
//        }
//        // end of block

//        Transform oldContainer = barRT.Find("PreviewContainer");
//        if (oldContainer)
//            DestroyImmediate(oldContainer.gameObject);

//        if (config.enablePreviews)
//            CreateBarPreviews(barRT, content);
//    }

//    private void AddEntry(EventTrigger trig, EventTriggerType t, System.Action<BaseEventData> cb)
//    {
//        var e = new EventTrigger.Entry { eventID = t };
//        e.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
//        trig.triggers.Add(e);
//    }

//    private void OnBarPointerDown(BaseEventData bed)
//    {
//        var ped = (PointerEventData)bed;
//        GameObject go = ped.pointerPress ?? ped.pointerEnter;
//        RectTransform rt = FindBarRT(go);

//        Debug.Log($"Bar clicked: {go?.name}, RT found: {rt?.name}"); // DEBUG

//        if (!rt) return;

//        // Ctrl/Command => additive toggling
//        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
//                      Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

//        if (!ctrl && !selectedBars.Contains(rt) && !selecting)
//        {
//            ClearSelection();
//            SelectBar(rt, true);
//        }
//        else if (ctrl)
//        {
//            // toggle selection on ctrl-click
//            if (selectedBars.Contains(rt)) SelectBar(rt, false);
//            else SelectBar(rt, true);
//        }

//        if (inspectorPanel != null && barMap.TryGetValue(rt, out Content clickedContent))
//        {
//            inspectorPanel.Show(clickedContent);
//        }
//    }

//    private RectTransform FindBarRT(GameObject go)
//    {
//        RectTransform rt = null;
//        if (go != null)
//        {
//            var t = go.transform;
//            while (t != null)
//            {
//                rt = t as RectTransform;
//                if (rt && barMap.ContainsKey(rt)) break;
//                t = t.parent; rt = null;
//            }
//        }
//        return rt;
//    }

//    private void OnBarBeginDrag(BaseEventData bed)
//    {
//        var ped = (PointerEventData)bed;
//        draggingBar = FindBarRT(ped.pointerPress ?? ped.pointerEnter);

//        Debug.Log($"Begin drag for bar: {draggingBar?.name}"); // DEBUG

//        if (!draggingBar) return;

//        int trackIndex = GetRowIndexOf(draggingBar.parent as RectTransform);
//        if (trackIndex != -1 && trackIndex < trackLockedStates.Count && trackLockedStates[trackIndex])
//        {
//            draggingBar = null;
//            return;
//        }

//        Vector2 currentPos = draggingBar.anchoredPosition;
//        Debug.Log($"Current position: {currentPos}"); // DEBUG

//        RectTransform parent = draggingBar.parent as RectTransform;
//        ScreenToLocal(parent, ped.position, out var lp);
//        barGrabOffsetX = lp.x - draggingBar.anchoredPosition.x;

//        // group?
//        draggingGroup = selectedBars.Contains(draggingBar) && selectedBars.Count > 1;
//        groupStartPos.Clear();
//        if (draggingGroup)
//        {
//            foreach (var rt in selectedBars)
//                if (rt) groupStartPos[rt] = rt.anchoredPosition;
//        }
//        else
//        {
//            if (!selectedBars.Contains(draggingBar))
//            {
//                ClearSelection();
//                SelectBar(draggingBar, true);
//            }
//        }
//    }

//    private void OnBarDrag(BaseEventData bed)
//    {
//        if (!draggingBar) return;
//        var ped = (PointerEventData)bed;

//        RectTransform parentRT = draggingBar.parent as RectTransform;
//        ScreenToLocal(parentRT, ped.position, out var lp);
//        float desiredX = lp.x - barGrabOffsetX;

//        float maxX = Mathf.Max(0f, parentRT.rect.width - draggingBar.rect.width);
//        desiredX = Mathf.Clamp(desiredX, 0f, maxX);

//        if (draggingGroup)
//        {
//            float dx = desiredX - draggingBar.anchoredPosition.x;
//            foreach (var rt in selectedBars)
//            {
//                if (!rt) continue;
//                var row = (RectTransform)rt.parent;
//                float rowMax = Mathf.Max(0f, row.rect.width - rt.rect.width);
//                float newX = Mathf.Clamp(rt.anchoredPosition.x + dx, 0f, rowMax);
//                rt.anchoredPosition = new Vector2(newX, 0f);
//            }
//        }
//        else
//        {
//            draggingBar.anchoredPosition = new Vector2(desiredX, 0f);
//            MaybeMoveBarToRowUnderPointer(ped.position);
//        }
//    }

//    private void OnBarEndDrag(BaseEventData bed)
//    {
//        if (!draggingBar) return;

//        if (draggingGroup)
//        {
//            foreach (var rt in selectedBars)
//            {
//                var content = barMap[rt];
//                RectTransform row = (RectTransform)rt.parent;

//                float startT = XToTime(rt.anchoredPosition.x);
//                float duration = GetContentDuration(content);

//                float snapped = ComputeSnappedTime(startT, content);
//                float resolved = ResolveNoOverlapStart(row, content, snapped, duration);

//                content.SetStart(resolved);
//                rt.anchoredPosition = new Vector2(TimeToX(resolved), 0f);

//                EnsureWidthForTime(resolved + duration);
//            }
//        }
//        else
//        {
//            var content = barMap[draggingBar];
//            var parentRT = (RectTransform)draggingBar.parent;

//            float startT = XToTime(draggingBar.anchoredPosition.x);
//            float duration = GetContentDuration(content);

//            float snapped = ComputeSnappedTime(startT, content);
//            float resolved = ResolveNoOverlapStart(parentRT, content, snapped, duration);

//            content.SetStart(resolved);
//            draggingBar.anchoredPosition = new Vector2(TimeToX(resolved), 0f);

//            EnsureWidthForTime(resolved + duration);
//        }

//        SetTime(GetTime()); // reflect immediately

//        draggingBar = null;
//        draggingGroup = false;
//        UpdateRowCloseButtons();
//    }

//    // ---------- Background selection (row triggers) ----------
//    private void AttachRowTriggers(RectTransform row)
//    {
//        var trig = row.gameObject.GetComponent<EventTrigger>() ?? row.gameObject.AddComponent<EventTrigger>();
//        AddEntry(trig, EventTriggerType.PointerDown, OnBackgroundPointerDown);
//        AddEntry(trig, EventTriggerType.Drag, OnBackgroundDrag);
//        AddEntry(trig, EventTriggerType.PointerUp, OnBackgroundPointerUp);
//    }

//    private void OnBackgroundPointerDown(BaseEventData bed)
//    {
//        var ped = (PointerEventData)bed;

//        // if pointer is over a bar, let the bar handle it
//        if (FindBarRT(ped.pointerEnter)) return;

//        additiveSelect = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
//                           Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

//        selecting = true;
//        selectStartScreen = ped.position;

//        if (!additiveSelect) ClearSelection();

//        ShowMarquee(selectStartScreen, selectStartScreen);
//    }

//    private void OnBackgroundDrag(BaseEventData bed)
//    {
//        if (!selecting) return;
//        var ped = (PointerEventData)bed;
//        ShowMarquee(selectStartScreen, ped.position);
//        UpdateSelectionFromMarquee(additiveSelect);
//    }

//    private void OnBackgroundPointerUp(BaseEventData bed)
//    {
//        if (!selecting) return;
//        selecting = false;
//        marqueeRT.gameObject.SetActive(false);
//    }

//    // ---------- IMAGE BAR RESIZE HANDLES ----------
//    private void AddBarResizeHandle(RectTransform barRT, Content content, bool left)
//    {
//        var go = new GameObject(left ? "ResizeL" : "ResizeR",
//            typeof(RectTransform), typeof(Image), typeof(EventTrigger));
//        var rt = go.GetComponent<RectTransform>();
//        var img = go.GetComponent<Image>();
//        var et = go.GetComponent<EventTrigger>();

//        go.transform.SetParent(barRT, false);
//        const float gripW = 10f;

//        if (left)
//        {
//            rt.anchorMin = new Vector2(0, 0);
//            rt.anchorMax = new Vector2(0, 1);
//            rt.pivot = new Vector2(0.5f, 0.5f);
//            rt.sizeDelta = new Vector2(gripW, 0);
//            rt.anchoredPosition = new Vector2(gripW * 0.5f, 0);
//        }
//        else
//        {
//            rt.anchorMin = new Vector2(1, 0);
//            rt.anchorMax = new Vector2(1, 1);
//            rt.pivot = new Vector2(0.5f, 0.5f);
//            rt.sizeDelta = new Vector2(gripW, 0);
//            rt.anchoredPosition = new Vector2(-gripW * 0.5f, 0);
//        }

//        img.color = new Color(1, 1, 1, 0.001f);
//        img.raycastTarget = true;

//        var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
//        begin.callback.AddListener(_ => { resizingBar = barRT; resizingLeft = left; });
//        et.triggers.Add(begin);

//        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
//        drag.callback.AddListener(ev => OnBarResizeDrag((PointerEventData)ev, content));
//        et.triggers.Add(drag);

//        var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
//        end.callback.AddListener(_ => { OnBarResizeEnd(content); resizingBar = null; });
//        et.triggers.Add(end);
//    }

//    private void OnBarResizeDrag(PointerEventData ped, Content content)
//    {
//        if (!resizingBar) return;

//        var parentRT = (RectTransform)resizingBar.parent;
//        float scale = (cachedCanvas ? cachedCanvas.scaleFactor : 1f);
//        float dx = ped.delta.x / Mathf.Max(0.0001f, scale);

//        float minW = (config.minImageDurationSeconds / 4f) * config.gridCellHorizontalPixelCount;

//        float newWidth = resizingBar.rect.width + (resizingLeft ? -dx : dx);
//        newWidth = Mathf.Max(minW, newWidth);

//        if (resizingLeft)
//        {
//            float newX = Mathf.Clamp(resizingBar.anchoredPosition.x + dx, 0f,
//                                      Mathf.Max(0f, parentRT.rect.width - newWidth));
//            resizingBar.anchoredPosition = new Vector2(newX, 0f);
//        }

//        resizingBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
//    }

//    private void OnBarResizeEnd(Content content)
//    {
//        if (!resizingBar) return;

//        float x = resizingBar.anchoredPosition.x;
//        float w = resizingBar.rect.width;

//        float startT = XToTime(x);
//        float durT = XToTime(w) - XToTime(0f);

//        if (resizingLeft)
//        {
//            float snappedStart = ComputeSnappedTime(startT, content);
//            content.SetStart(snappedStart);

//            float endT = startT + durT;
//            durT = Mathf.Max(config.minImageDurationSeconds, endT - snappedStart);

//            resizingBar.anchoredPosition = new Vector2(TimeToX(snappedStart), 0f);
//            resizingBar.SetSizeWithCurrentAnchors(
//                RectTransform.Axis.Horizontal, TimeToX(durT) - TimeToX(0f));
//        }
//        else
//        {
//            float endT = startT + durT;
//            float snappedEnd = ComputeSnappedTime(endT, content);
//            durT = Mathf.Max(config.minImageDurationSeconds, snappedEnd - content.getStart());

//            resizingBar.SetSizeWithCurrentAnchors(
//                RectTransform.Axis.Horizontal, TimeToX(durT) - TimeToX(0f));
//        }

//        imageLengths[content] = durT;

//        EnsureContentWidthForBar(resizingBar);
//        EnsureWidthForTime(content.getStart() + durT);

//        SetTime(GetTime());
//    }

//    // ---------- Snapping, overlap, and sizing ----------
//    private float ComputeSnappedTime(float t, Content exclude)
//    {
//        float gridSecs = 4f;
//        float tGrid = Mathf.Round(t / gridSecs) * gridSecs;

//        var candidates = GetSnapTimes(exclude);
//        float nearest = tGrid;
//        float bestDist = Mathf.Abs(nearest - t);

//        foreach (var st in candidates)
//        {
//            float d = Mathf.Abs(st - t);
//            if (d < bestDist) { bestDist = d; nearest = st; }
//        }

//        float thrTime = XToTime(config.snapPixelThreshold);
//        return (bestDist <= thrTime) ? Mathf.Max(0f, nearest) : Mathf.Max(0f, t);
//    }

//    public List<float> GetSnapTimes(Content exclude = null)
//    {
//        var list = new List<float> { 0f };
//        for (int i = 0; i < videoControllerList.Count; i++)
//        {
//            var c = videoControllerList[i].getvc();
//            if (c == null || c == exclude) continue;
//            list.Add(c.getStart()); list.Add(c.getEnd());
//        }
//        for (int i = 0; i < imageControllerList.Count; i++)
//        {
//            var c = imageControllerList[i].getic();
//            if (c == null || c == exclude) continue;
//            float len = GetLengthOverride(c, c.getLength());
//            list.Add(c.getStart()); list.Add(c.getStart() + len);
//        }
//        for (int i = 0; i < audioControllerList.Count; i++)
//        {
//            var c = audioControllerList[i].getac();
//            if (c == null || c == exclude) continue;
//            list.Add(c.getStart()); list.Add(c.getEnd());
//        }
//        // Also add snapping for models
//        for (int i = 0; i < modelControllerList.Count; i++)
//        {
//            var c = modelControllerList[i].getmc();
//            if (c == null || c == exclude) continue;
//            list.Add(c.getStart()); list.Add(c.getEnd());
//        }
//        return list;
//    }

//    private float GetContentWidth() => scrollViewContent ? scrollViewContent.rect.width : 0f;

//    private void SetContentWidth(float width)
//    {
//        if (!scrollViewContent) return;
//        scrollViewContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
//        for (int i = 0; i < trackRows.Count; i++)
//            trackRows[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
//    }

//    private void EnsureContentWidthForBar(RectTransform barRT)
//    {
//        if (!barRT) return;
//        float requiredRight = barRT.anchoredPosition.x + barRT.rect.width + config.contentRightMarginPx;
//        if (requiredRight > GetContentWidth())
//        {
//            SetContentWidth(requiredRight);
//            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
//            if (tracksRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
//        }
//    }

//    private void UpdateContentHeight()
//    {
//        if (!scrollViewContent) return;
//        int n = Mathf.Max(1, trackRows.Count);
//        float rowsH = (n * config.laneHeight) + ((n - 1) * config.laneSpacing);
//        float target = rowsH + config.verticalMargin + config.rulerHeight;

//        scrollViewContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target);
//        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
//    }

//    // ---------- Windows ----------
//    public void ShowContent(Content c, Texture tex)
//    {
//        var w = EnsureWindow(c, tex);
//        if (w == null) return;
//        if (!w.root.gameObject.activeSelf) w.root.gameObject.SetActive(true);
//        if (tex) w.view.texture = tex;
//        w.view.enabled = true;
//        w.fitter?.FitNow(w.view.texture);
//        UpdateWindowOrders();
//    }

//    public void HideContent(Content c)
//    {
//        if (c != null && windows.TryGetValue(c, out var w) && w.root != null)
//            w.root.gameObject.SetActive(false);
//    }

//    private Win EnsureWindow(Content c, Texture tex)
//    {
//        if (!playbackStackRoot) return null;
//        if (windows.TryGetValue(c, out var have))
//        {
//            if (tex) have.view.texture = tex;
//            return have;
//        }

//        var rootGO = new GameObject($"Win_{c.GetHashCode()}",
//            typeof(RectTransform), typeof(Image), typeof(WindowResizer));
//        var rootRT = rootGO.GetComponent<RectTransform>();
//        rootRT.SetParent(playbackStackRoot, false);
//        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = new Vector2(0.5f, 0.5f);

//        var pSize = playbackStackRoot.rect.size;
//        rootRT.sizeDelta = pSize * 0.7f;
//        rootRT.anchoredPosition = Vector2.zero;

//        var bg = rootGO.GetComponent<Image>();
//        bg.color = new Color(0f, 0f, 0f, 0.12f);
//        bg.raycastTarget = true;

//        var resizer = rootGO.GetComponent<WindowResizer>();
//        resizer.target = rootRT;
//        resizer.bounds = playbackStackRoot;
//        resizer.minWidth = 160f;
//        resizer.minHeight = 90f;
//        resizer.edge = 12f;
//        resizer.allowMove = true;

//        var viewGO = new GameObject("View", typeof(RectTransform), typeof(RawImage), typeof(RawImageFitter));
//        var viewRT = viewGO.GetComponent<RectTransform>();
//        viewRT.SetParent(rootRT, false);
//        viewRT.anchorMin = Vector2.zero; viewRT.anchorMax = Vector2.one;
//        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

//        var ri = viewGO.GetComponent<RawImage>();
//        ri.texture = tex;
//        ri.raycastTarget = false;
//        ri.enabled = false;

//        var fitter = viewGO.GetComponent<RawImageFitter>();
//        fitter.referenceBounds = rootRT;
//        fitter.mode = RawImageFitter.FitMode.Contain;
//        fitter.fitBlackTexture = false;

//        var win = new Win { root = rootRT, view = ri, fitter = fitter };
//        windows[c] = win;

//        UpdateWindowOrders();
//        rootGO.SetActive(false);
//        return win;
//    }

//    private int RowIndexFor(Content c)
//    {
//        if (c != null && contentToBar.TryGetValue(c, out var bar) && bar && bar.parent is RectTransform row)
//            return GetRowIndexOf(row);
//        return int.MaxValue;
//    }

//    private void UpdateWindowOrders()
//    {
//        if (windows.Count == 0) return;

//        var ordered = new List<KeyValuePair<Content, Win>>(windows);
//        ordered.Sort((a, b) => RowIndexFor(b.Key).CompareTo(RowIndexFor(a.Key))); // bottom first, top last
//        foreach (var pair in ordered) pair.Value.root.SetAsLastSibling();
//    }

//    // ---------- Overlap control ----------
//    private float GetContentDuration(Content c)
//    {
//        if (c is ImageContent ic) return GetLengthOverride(ic, ic.getLength());
//        return Mathf.Max(0f, c.getEnd() - c.getStart());
//    }

//    private float ResolveNoOverlapStart(RectTransform row, Content moving, float desiredStart, float duration)
//    {
//        var intervals = new List<(float s, float e)>();
//        foreach (var kv in contentToBar)
//        {
//            if (!kv.Value || kv.Value.parent != row || kv.Key == moving) continue;
//            float s = kv.Key.getStart();
//            float d = GetContentDuration(kv.Key);
//            intervals.Add((s, s + d));
//        }
//        intervals.Sort((a, b) => a.s.CompareTo(b.s));

//        if (Fits(intervals, desiredStart, desiredStart + duration))
//            return Mathf.Max(0f, desiredStart);

//        for (int i = 0; i <= intervals.Count; i++)
//        {
//            float gapStart = (i == 0) ? 0f : intervals[i - 1].e;
//            float gapEnd = (i == intervals.Count) ? float.PositiveInfinity : intervals[i].s;

//            float candidate = Mathf.Max(desiredStart, gapStart);
//            if (candidate + duration <= gapEnd)
//                return candidate;
//        }
//        return intervals.Count > 0 ? intervals[^1].e : Mathf.Max(0f, desiredStart);
//    }

//    private bool Fits(List<(float s, float e)> ints, float s, float e)
//    {
//        for (int i = 0; i < ints.Count; i++)
//        {
//            if (e <= ints[i].s) return true;
//            if (s < ints[i].e && e > ints[i].s) return false; // overlap
//        }
//        return true;
//    }

//    // ---------- Empty row close button ----------
//    private void AddCloseButtonToRow(RectTransform row)
//    {
//        var btnGO = new GameObject("CloseEmpty", typeof(RectTransform), typeof(Image), typeof(Button));
//        var rt = btnGO.GetComponent<RectTransform>();
//        rt.SetParent(row, false);
//        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
//        rt.pivot = new Vector2(1, 1);
//        rt.sizeDelta = new Vector2(18, 18);
//        rt.anchoredPosition = new Vector2(-4, -4);

//        var img = btnGO.GetComponent<Image>();
//        img.color = new Color(0, 0, 0, 0.6f);

//        var txtGO = new GameObject("X", typeof(RectTransform), typeof(Text));
//        var txtRT = txtGO.GetComponent<RectTransform>();
//        txtRT.SetParent(rt, false);
//        txtRT.anchorMin = txtRT.anchorMax = new Vector2(0.5f, 0.5f);
//        txtRT.pivot = new Vector2(0.5f, 0.5f);
//        txtRT.sizeDelta = Vector2.zero;

//        var t = txtGO.GetComponent<Text>();
//        t.text = "×";
//        t.alignment = TextAnchor.MiddleCenter;
//        t.color = Color.white;
//        t.fontSize = 14;
//        t.raycastTarget = false;

//        var b = btnGO.GetComponent<Button>();
//        b.onClick.AddListener(() =>
//        {
//            if (RowHasBars(row)) return; // only close empty rows
//            RemoveRow(row);
//        });

//        btnGO.SetActive(false); // visible only when row is empty
//    }

//    private bool RowHasBars(RectTransform row)
//    {
//        foreach (var kv in contentToBar)
//            if (kv.Value && kv.Value.parent == row) return true;
//        return false;
//    }

//    private void RemoveRow(RectTransform row)
//    {
//        if (row == null) return;
//        if (RowHasBars(row)) return;
//        int idx = GetRowIndexOf(row);
//        if (idx < 0) return;

//        trackRows.RemoveAt(idx);
//        Destroy(row.gameObject);
//        UpdateContentHeight();
//        UpdateRowCloseButtons();
//    }

//    private void UpdateRowCloseButtons()
//    {
//        foreach (var row in trackRows)
//        {
//            var close = row.Find("CloseEmpty");
//            if (!close) continue;
//            close.gameObject.SetActive(!RowHasBars(row));
//        }
//    }

//    // ---------- Selection overlay + marquee ----------
//    private void EnsureSelectionOverlay()
//    {
//        // separate overlay so it draws ABOVE rows and is not affected by VerticalLayoutGroup
//        var go = new GameObject("SelectionOverlay", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
//        selectionOverlayRT = go.GetComponent<RectTransform>();
//        selectionOverlayRT.SetParent(scrollViewContent, false);

//        // match scrollViewContent full rect
//        selectionOverlayRT.anchorMin = new Vector2(0, 1);
//        selectionOverlayRT.anchorMax = new Vector2(0, 1);
//        selectionOverlayRT.pivot = new Vector2(0, 1);
//        selectionOverlayRT.anchoredPosition = Vector2.zero;
//        selectionOverlayRT.sizeDelta = scrollViewContent.sizeDelta; // updated when we expand
//        var le = go.GetComponent<LayoutElement>(); le.ignoreLayout = true;

//        var img = go.GetComponent<Image>();
//        img.color = new Color(0, 0, 0, 0); // invisible
//        img.raycastTarget = false;       // do NOT block bar/row events
//    }

//    private void EnsureMarquee()
//    {
//        var go = new GameObject("Marquee", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
//        marqueeRT = go.GetComponent<RectTransform>();
//        marqueeRT.SetParent(selectionOverlayRT, false);
//        marqueeRT.anchorMin = marqueeRT.anchorMax = new Vector2(0, 1);
//        marqueeRT.pivot = new Vector2(0, 1);
//        marqueeRT.sizeDelta = Vector2.zero;
//        marqueeRT.anchoredPosition = Vector2.zero;

//        var le = go.GetComponent<LayoutElement>(); le.ignoreLayout = true;

//        var img = go.GetComponent<Image>();
//        img.color = new Color(0.2f, 0.6f, 1f, 0.25f);
//        img.raycastTarget = false;
//        marqueeRT.gameObject.SetActive(false);
//    }

//    private void ShowMarquee(Vector2 startScreen, Vector2 endScreen)
//    {
//        // convert to selectionOverlay local (so it sits above everything visually)
//        ScreenToLocal(selectionOverlayRT, startScreen, out var a);
//        ScreenToLocal(selectionOverlayRT, endScreen, out var b);

//        float minX = Mathf.Min(a.x, b.x);
//        float maxX = Mathf.Max(a.x, b.x);
//        float minY = Mathf.Min(a.y, b.y);
//        float maxY = Mathf.Max(a.y, b.y);

//        marqueeRT.gameObject.SetActive(true);
//        marqueeRT.anchoredPosition = new Vector2(minX, maxY);
//        marqueeRT.sizeDelta = new Vector2(maxX - minX, maxY - minY);

//        // keep overlay & marquee on top
//        selectionOverlayRT.SetAsLastSibling();
//        marqueeRT.SetAsLastSibling();
//    }

//    private void UpdateSelectionFromMarquee(bool additive)
//    {
//        if (!additive) ClearSelection();

//        var marCorners = new Vector3[4];
//        marqueeRT.GetWorldCorners(marCorners);
//        Rect marRect = new Rect(marCorners[0], marCorners[2] - marCorners[0]); // screen space

//        foreach (var kv in barMap)
//        {
//            var rt = kv.Key;
//            if (!rt || !rt.gameObject.activeInHierarchy) continue;

//            var bc = new Vector3[4];
//            rt.GetWorldCorners(bc);
//            Rect barRect = new Rect(bc[0], bc[2] - bc[0]);

//            if (barRect.Overlaps(marRect)) SelectBar(rt, true);
//        }
//    }

//    private void SelectBar(RectTransform rt, bool selected)
//    {
//        if (!rt) return;
//        var img = rt.GetComponent<Image>();
//        var outline = rt.GetComponent<Outline>();
//        if (selected)
//        {
//            selectedBars.Add(rt);
//            if (!outline) outline = rt.gameObject.AddComponent<Outline>();
//            outline.effectColor = new Color(1f, 1f, 1f, 0.9f);
//            outline.effectDistance = new Vector2(2, -2);
//            if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
//        }
//        else
//        {
//            selectedBars.Remove(rt);
//            if (outline) Destroy(outline);
//        }
//    }

//    private void ClearSelection()
//    {
//        foreach (var rt in new List<RectTransform>(selectedBars))
//            SelectBar(rt, false);
//        selectedBars.Clear();
//    }

//    // ---------- helpers ----------
//    private void FixContentAnchors()
//    {
//        var rt = scrollViewContent;
//        rt.anchorMin = new Vector2(0f, 1f);
//        rt.anchorMax = new Vector2(0f, 1f);
//        rt.pivot = new Vector2(0f, 1f);
//        rt.anchoredPosition = Vector2.zero;
//        if (rt.rect.width <= 0f)
//            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800f);
//    }

//    private void ScreenToLocal(RectTransform target, Vector2 screen, out Vector2 local)
//    {
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            target, screen, cachedCanvas ? cachedCanvas.worldCamera : null, out local);
//    }

//    private int CountEmptyRows()
//    {
//        int count = 0;
//        foreach (var row in trackRows)
//            if (!RowHasBars(row)) count++;
//        return count;
//    }

//    private void MaybeMoveBarToRowUnderPointer(Vector2 screenPos)
//    {
//        if (draggingGroup) return; // group stays in its rows
//        if (trackRows.Count == 0) return;

//        var cam = cachedCanvas ? cachedCanvas.worldCamera : null;

//        var row = GetRowUnderPointer(screenPos);
//        if (row != null)
//        {
//            if (draggingBar.parent != row)
//            {
//                draggingBar.SetParent(row, false);
//                draggingBar.localScale = Vector3.one;
//                draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
//            }
//            return;
//        }

//        bool canCreateMoreEmpty = CountEmptyRows() == 0;

//        var topRow = trackRows[0];
//        var topCorners = new Vector3[4]; topRow.GetWorldCorners(topCorners);
//        float topEdgeY = RectTransformUtility.WorldToScreenPoint(cam, topCorners[1]).y;
//        if (screenPos.y > topEdgeY + config.laneHeight * 0.25f)
//        {
//            if (canCreateMoreEmpty)
//            {
//                var newTop = InsertRowAt(0);
//                draggingBar.SetParent(newTop, false);
//            }
//            else
//            {
//                draggingBar.SetParent(topRow, false);
//            }
//            draggingBar.localScale = Vector3.one;
//            draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
//            return;
//        }

//        var bottomRow = trackRows[^1];
//        var bottomCorners = new Vector3[4]; bottomRow.GetWorldCorners(bottomCorners);
//        float bottomEdgeY = RectTransformUtility.WorldToScreenPoint(cam, bottomCorners[0]).y;

//        if (screenPos.y < bottomEdgeY - config.laneHeight * 0.25f)
//        {
//            if (canCreateMoreEmpty)
//            {
//                EnsureRowsAtLeast(trackRows.Count + 1);
//                var newBottom = trackRows[^1];
//                draggingBar.SetParent(newBottom, false);
//            }
//            else
//            {
//                draggingBar.SetParent(bottomRow, false);
//            }
//            draggingBar.localScale = Vector3.one;
//            draggingBar.anchoredPosition = new Vector2(draggingBar.anchoredPosition.x, 0f);
//        }
//    }

//    private RectTransform GetRowUnderPointer(Vector2 screenPos)
//    {
//        if (trackRows.Count == 0) return null;
//        var cam = cachedCanvas ? cachedCanvas.worldCamera : null;
//        foreach (var row in trackRows)
//            if (RectTransformUtility.RectangleContainsScreenPoint(row, screenPos, cam))
//                return row;
//        return null;
//    }

//    private void HandleZoomInput()
//    {
//        if (Input.mouseScrollDelta.y != 0f)
//        {
//            Vector2 mousePos = Input.mousePosition;
//            if (IsMouseOverTimeline(mousePos))
//            {
//                ZoomTimeline(Input.mouseScrollDelta.y, mousePos);
//            }
//        }
//    }

//    private bool IsMouseOverTimeline(Vector2 mousePos)
//    {
//        return RectTransformUtility.RectangleContainsScreenPoint(
//            scrollViewContent, mousePos, cachedCanvas?.worldCamera);
//    }

//    private void ZoomTimeline(float zoomDelta, Vector2 mousePos)
//    {
//        // change mouse position to local point in scrollViewContent
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            scrollViewContent, mousePos, cachedCanvas?.worldCamera, out Vector2 localPoint);

//        float mouseTimeBeforeZoom = XToTime(localPoint.x);

//        // change zoom level
//        float zoomFactor = zoomDelta > 0 ? config.zoomSpeed : 1f / config.zoomSpeed;
//        float newZoom = Mathf.Clamp(config.gridCellHorizontalPixelCount * zoomFactor, config.minZoom, config.maxZoom);

//        if (Mathf.Abs(newZoom - config.gridCellHorizontalPixelCount) < 0.1f) return;

//        config.gridCellHorizontalPixelCount = (int)newZoom;

//        RefreshAllBarsAfterZoom();

//        MaintainMousePosition(mouseTimeBeforeZoom, localPoint);
//    }

//    private void RefreshAllBarsAfterZoom()
//    {
//        foreach (var controller in videoControllerList)
//        {
//            var content = controller.getvc();
//            var bar = contentToBar.GetValueOrDefault(content);
//            if (bar) UpdateBarPosition(bar, content);
//        }

//        foreach (var controller in imageControllerList)
//        {
//            var content = controller.getic();
//            var bar = contentToBar.GetValueOrDefault(content);
//            if (bar) UpdateBarPosition(bar, content);
//        }

//        foreach (var controller in audioControllerList)
//        {
//            var content = controller.getac();
//            var bar = contentToBar.GetValueOrDefault(content);
//            if (bar) UpdateBarPosition(bar, content);
//        }

//        foreach (var controller in modelControllerList)
//        {
//            var content = controller.getmc();
//            var bar = contentToBar.GetValueOrDefault(content);
//            if (bar) UpdateBarPosition(bar, content);
//        }

//        UpdateContentWidthAfterZoom();
//        UpdateTimeRuler();

//    }

//    private void UpdateBarPosition(RectTransform bar, Content content)
//    {
//        if (!bar || content == null) return;

//        float startX = TimeToX(content.getStart());
//        float duration = GetContentDuration(content);
//        float width = TimeToX(duration) - TimeToX(0f);

//        bar.anchoredPosition = new Vector2(startX, bar.anchoredPosition.y);
//        bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

//        RefreshBarPreviews(bar, content);
//    }

//    private void UpdateContentWidthAfterZoom()
//    {
//        float maxEndTime = 0f;

//        foreach (var controller in videoControllerList)
//            maxEndTime = Mathf.Max(maxEndTime, controller.getvc().getEnd());

//        foreach (var controller in imageControllerList)
//            maxEndTime = Mathf.Max(maxEndTime, controller.getic().getEnd());

//        foreach (var controller in audioControllerList)
//            maxEndTime = Mathf.Max(maxEndTime, controller.getac().getEnd());

//        foreach (var controller in modelControllerList)
//            maxEndTime = Mathf.Max(maxEndTime, controller.getmc().getEnd());

//        if (maxEndTime > 0f)
//            EnsureWidthForTime(maxEndTime);
//    }

//    private void MaintainMousePosition(float mouseTime, Vector2 originalLocalPoint)
//    {
//        if (!parentScrollRect) return;

//        float oldMouseX = originalLocalPoint.x;
//        float newMouseX = TimeToX(mouseTime);
//        float deltaX = newMouseX - oldMouseX;

//        float contentWidth = scrollViewContent.rect.width;
//        float viewportWidth = parentScrollRect.viewport.rect.width;

//        if (contentWidth > viewportWidth)
//        {
//            float maxScroll = contentWidth - viewportWidth;
//            float currentScroll = parentScrollRect.horizontalNormalizedPosition * maxScroll;
//            float newScroll = Mathf.Clamp(currentScroll + deltaX, 0f, maxScroll);

//            parentScrollRect.horizontalNormalizedPosition = newScroll / maxScroll;
//        }
//    }

//    private void CreateTimeRuler()
//    {
//        GameObject rulerGO = new GameObject("TimeRuler", typeof(RectTransform), typeof(Image));
//        timeRulerRT = rulerGO.GetComponent<RectTransform>();
//        timeRulerRT.SetParent(scrollViewContent, false);

//        timeRulerRT.anchorMin = new Vector2(0, 1);
//        timeRulerRT.anchorMax = new Vector2(0, 1);
//        timeRulerRT.pivot = new Vector2(0, 0);
//        timeRulerRT.anchoredPosition = new Vector2(0, 0);
//        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetContentWidth());
//        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, config.rulerHeight);

//        Image bg = rulerGO.GetComponent<Image>();
//        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
//        bg.raycastTarget = false;

//        // place to change the font for time text
//        timeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

//        if (tracksRoot)
//        {
//            tracksRoot.anchoredPosition = new Vector2(0, -config.rulerHeight);
//        }
//    }

//    // TimelineGrid.cs içindeki bu fonksiyonu GÜNCELLEYİN
//    private void UpdateTimeRuler()
//    {
//        if (!timeRulerRT) return;

//        foreach (var marker in timeMarkers)
//        {
//            if (marker) Destroy(marker); // DestroyImmediate yerine Destroy kullanmak daha güvenli olabilir
//        }
//        timeMarkers.Clear();

//        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetContentWidth());

//        // Zoom seviyesine göre daha hassas aralıklar
//        float pixelsPerSecond = config.gridCellHorizontalPixelCount / 4f;
//        float targetPixelInterval = 80f; // Aralıkları biraz daha açalım
//        float secondsPerInterval = targetPixelInterval / pixelsPerSecond;

//        // Güzel, yuvarlak sayılar bulmak için bir mantık
//        // Örn: 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60...
//        float[] niceIntervals = { 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f, 15f, 30f, 60f };
//        float bestInterval = 60f;
//        foreach (float interval in niceIntervals)
//        {
//            if (secondsPerInterval < interval)
//            {
//                bestInterval = interval;
//                break;
//            }
//        }

//        float contentWidth = GetContentWidth();
//        float maxTime = XToTime(contentWidth);
//        int subDivisions = 5; // Her ana çizgi arasına kaç küçük çizgi atılacağı (genellikle 5 veya 10)

//        for (float time = 0; time <= maxTime; time += bestInterval)
//        {
//            // Ana çizgileri ve metinleri oluştur
//            CreateTimeMarker(time, true);

//            // Ara çizgileri oluştur
//            for (int i = 1; i < subDivisions; i++)
//            {
//                float minorTime = time + (bestInterval / subDivisions) * i;
//                if (minorTime <= maxTime)
//                {
//                    CreateTimeMarker(minorTime, false);
//                }
//            }
//        }
//    }
//    // TimelineGrid.cs içindeki bu iki fonksiyonu aşağıdaki kodlarla değiştirin.

//    private void CreateTimeMarker(float timeSeconds, bool isMajorTick)
//    {
//        float xPos = TimeToX(timeSeconds);

//        GameObject tickGO = new GameObject($"Tick_{timeSeconds:F1}s", typeof(RectTransform), typeof(Image));
//        RectTransform tickRT = tickGO.GetComponent<RectTransform>();
//        tickRT.SetParent(timeRulerRT, false);
//        tickRT.anchorMin = new Vector2(0, 0);
//        tickRT.anchorMax = new Vector2(0, 1);
//        tickRT.pivot = new Vector2(0.5f, 0);
//        tickRT.anchoredPosition = new Vector2(xPos, 0);

//        // --- DEĞİŞİKLİK 1: Ana çizgileri daha uzun yapma ---
//        // Ana çizgiler cetvelin %80'ini, ara çizgiler %40'ını kaplayacak.
//        // Bu, aradaki farkı belirgin hale getirir.
//        float tickWidth = isMajorTick ? 1f : 1f;
//        float tickHeightFactor = isMajorTick ? 0.8f : 0.3f; // Değerleri biraz artırdık

//        tickRT.sizeDelta = new Vector2(tickWidth, config.rulerHeight * tickHeightFactor);

//        Image tickImg = tickGO.GetComponent<Image>();
//        tickImg.color = isMajorTick ? config.majorTickColor : config.minorTickColor;
//        tickImg.raycastTarget = false;
//        timeMarkers.Add(tickGO);

//        if (isMajorTick)
//        {
//            CreateTimeText(timeSeconds, xPos);
//        }
//    }


//    private void CreateTimeText(float timeSeconds, float xPos)
//    {
//        GameObject textGO = new GameObject($"TimeText_{timeSeconds:F1}s", typeof(RectTransform), typeof(TextMeshProUGUI));
//        RectTransform textRT = textGO.GetComponent<RectTransform>();
//        textRT.SetParent(timeRulerRT, false);

//        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
//        text.text = FormatTime(timeSeconds);
//        text.fontSize = 12;
//        text.color = config.timeTextColor;
//        text.alignment = TextAlignmentOptions.Center;
//        text.raycastTarget = false;
//        text.overflowMode = TextOverflowModes.Overflow;
//        text.enableKerning = false;

//        float preferredWidth = text.preferredWidth;

//        textRT.sizeDelta = new Vector2(preferredWidth + 4f, 20f);

//        textRT.anchorMin = new Vector2(0, 1);
//        textRT.anchorMax = new Vector2(0, 1);
//        textRT.pivot = new Vector2(0.5f, 0);
//        textRT.anchoredPosition = new Vector2(xPos, 20f);

//        timeMarkers.Add(textGO);
//    }

//    private string FormatTime(float timeInSeconds)
//    {
//        timeInSeconds = Mathf.Max(0, timeInSeconds);
//        int hours = Mathf.FloorToInt(timeInSeconds / 3600);
//        int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
//        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
//        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100) % 100); // 1000 yerine 100 kullanmak daha okunaklı

//        // Eğer proje 1 saati geçmiyorsa, saat hanesini göstermeye gerek yok
//        if (hours > 0)
//        {
//            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", hours, minutes, seconds, milliseconds);
//        }
//        else
//        {
//            return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
//        }
//    }

//    private bool IsIntervalMultiple(float value, float interval)
//    {
//        return Mathf.Abs(value % interval) < 0.01f;
//    }

//    // ============ XML SUPPORT METHODS ============

//    public void ClearAll()
//    {
//        // Stop playback first
//        stopVideo();

//        // Clear all controllers
//        foreach (var vc in videoControllerList)
//            if (vc != null) Destroy(vc.gameObject);
//        videoControllerList.Clear();

//        foreach (var ic in imageControllerList)
//            if (ic != null) Destroy(ic.gameObject);
//        imageControllerList.Clear();

//        foreach (var ac in audioControllerList)
//            if (ac != null) Destroy(ac.gameObject);
//        audioControllerList.Clear();

//        foreach (var mc in modelControllerList)
//            if (mc != null) Destroy(mc.gameObject);
//        modelControllerList.Clear();

//        // Clear bars
//        foreach (var kvp in contentToBar)
//            if (kvp.Value != null) Destroy(kvp.Value.gameObject);

//        barMap.Clear();
//        contentToBar.Clear();
//        windows.Clear();
//        imageLengths.Clear();

//        // Reset to 1 track
//        while (trackRows.Count > 1)
//        {
//            var row = trackRows[trackRows.Count - 1];
//            if (row != null) Destroy(row.gameObject);
//            trackRows.RemoveAt(trackRows.Count - 1);
//        }

//        UpdateContentHeight();
//        UpdateRowCloseButtons();
//    }

//    public Content GetLastAddedContent()
//    {
//        Content lastContent = null;
//        float latestTime = -1;

//        if (videoControllerList.Count > 0)
//        {
//            var vc = videoControllerList[videoControllerList.Count - 1].getvc();
//            if (vc != null) { lastContent = vc; latestTime = 0; }
//        }

//        if (imageControllerList.Count > 0)
//        {
//            var ic = imageControllerList[imageControllerList.Count - 1].getic();
//            if (ic != null) { lastContent = ic; latestTime = 0; }
//        }

//        if (audioControllerList.Count > 0)
//        {
//            var ac = audioControllerList[audioControllerList.Count - 1].getac();
//            if (ac != null) { lastContent = ac; latestTime = 0; }
//        }

//        if (modelControllerList.Count > 0)
//        {
//            var mc = modelControllerList[modelControllerList.Count - 1].getmc();
//            if (mc != null) { lastContent = mc; latestTime = 0; }
//        }

//        return lastContent;
//    }

//    public void SetContentStartTime(Content content, float startTime)
//    {
//        if (content == null) return;

//        content.SetStart(startTime);

//        if (contentToBar.TryGetValue(content, out var bar))
//        {
//            bar.anchoredPosition = new Vector2(TimeToX(startTime), 0);
//        }
//    }

//    public void SetLastImageDuration(float duration)
//    {
//        if (imageControllerList.Count > 0)
//        {
//            var lastController = imageControllerList[imageControllerList.Count - 1];
//            var content = lastController.getic();
//            if (content != null)
//            {
//                imageLengths[content] = duration;

//                // Update bar width
//                if (contentToBar.TryGetValue(content, out var bar))
//                {
//                    float width = TimeToX(duration) - TimeToX(0f);
//                    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
//                }
//            }
//        }
//    }

//    // TimelineGrid.cs'e eklenecek yeni public metodlar

//    // ============ TRACK MANAGEMENT ============

//    public int GetTrackCount()
//    {
//        return trackRows.Count;
//    }

//    public void CreateNewTrack()
//    {
//        EnsureRowsAtLeast(trackRows.Count + 1);
//    }

//    public void MoveContentToTrack(Content content, int targetTrackIndex)
//    {
//        if (content == null) return;

//        // Ensure track exists
//        EnsureRowsAtLeast(targetTrackIndex + 1);

//        if (targetTrackIndex >= trackRows.Count) return;

//        // Get current bar
//        if (!contentToBar.TryGetValue(content, out var bar)) return;

//        RectTransform targetRow = trackRows[targetTrackIndex];

//        // Move bar to new track
//        bar.SetParent(targetRow, false);
//        bar.localScale = Vector3.one;
//        bar.anchoredPosition = new Vector2(bar.anchoredPosition.x, 0f);

//        // Update content layer
//        content.SetLayer(targetTrackIndex);

//        UpdateWindowOrders();
//        UpdateRowCloseButtons();
//    }

//    // ============ WINDOW MANAGEMENT ============

//    public void SetWindowProperties(Content content, float posX, float posY, float width, float height)
//    {
//        if (content == null) return;

//        // Get window for content
//        if (windows.TryGetValue(content, out var win))
//        {
//            if (win.root != null)
//            {
//                // Apply resolution scaling if needed
//                float scaleFactorX = 1f;
//                float scaleFactorY = 1f;

//                // Optional: Scale based on current resolution vs saved resolution
//                // This would need the saved resolution from XML

//                win.root.anchoredPosition = new Vector2(posX * scaleFactorX, posY * scaleFactorY);
//                win.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * scaleFactorX);
//                win.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * scaleFactorY);

//                // Force update fitter
//                if (win.fitter != null)
//                {
//                    win.fitter.FitNow(win.view?.texture);
//                }
//            }
//        }
//    }

//    public void SetWindowAnchors(Content content, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
//    {
//        if (content == null) return;

//        if (windows.TryGetValue(content, out var win))
//        {
//            if (win.root != null)
//            {
//                win.root.anchorMin = anchorMin;
//                win.root.anchorMax = anchorMax;
//                win.root.pivot = pivot;
//            }
//        }
//    }

//    public Vector2 GetWindowPosition(Content content)
//    {
//        if (content != null && windows.TryGetValue(content, out var win))
//        {
//            if (win.root != null)
//                return win.root.anchoredPosition;
//        }
//        return Vector2.zero;
//    }

//    public Vector2 GetWindowSize(Content content)
//    {
//        if (content != null && windows.TryGetValue(content, out var win))
//        {
//            if (win.root != null)
//                return win.root.rect.size;
//        }
//        return new Vector2(100, 100);
//    }

//    // ============ LAYER FIX ============

//    public void FixContentLayers()
//    {
//        // This ensures all content has correct layer based on their actual track
//        foreach (var kvp in contentToBar)
//        {
//            var content = kvp.Key;
//            var bar = kvp.Value;

//            if (bar != null && bar.parent != null)
//            {
//                // Find which track this bar belongs to
//                for (int i = 0; i < trackRows.Count; i++)
//                {
//                    if (bar.parent == trackRows[i])
//                    {
//                        content.SetLayer(i);
//                        break;
//                    }
//                }
//            }
//        }

//        UpdateWindowOrders();
//    }

//    // ============ RESOLUTION SCALING ============

//    private static float savedResolutionWidth = 1920f;
//    private static float savedResolutionHeight = 1080f;

//    public void SetSavedResolution(float width, float height)
//    {
//        savedResolutionWidth = width;
//        savedResolutionHeight = height;
//    }

//    public Vector2 GetResolutionScale()
//    {
//        float currentWidth = Screen.width;
//        float currentHeight = Screen.height;

//        return new Vector2(
//            currentWidth / savedResolutionWidth,
//            currentHeight / savedResolutionHeight
//        );
//    }

//    public void ApplyResolutionScaling()
//    {
//        Vector2 scale = GetResolutionScale();

//        // Scale all windows
//        foreach (var kvp in windows)
//        {
//            var win = kvp.Value;
//            if (win.root != null)
//            {
//                // Scale position and size
//                Vector2 currentPos = win.root.anchoredPosition;
//                Vector2 currentSize = win.root.rect.size;

//                win.root.anchoredPosition = new Vector2(
//                    currentPos.x * scale.x,
//                    currentPos.y * scale.y
//                );

//                win.root.SetSizeWithCurrentAnchors(
//                    RectTransform.Axis.Horizontal,
//                    currentSize.x * scale.x
//                );

//                win.root.SetSizeWithCurrentAnchors(
//                    RectTransform.Axis.Vertical,
//                    currentSize.y * scale.y
//                );
//            }
//        }
//    }

//    public void SetTrackVisibility(int trackIndex, bool isVisible)
//    {
//        if (trackIndex < 0 || trackIndex >= trackRows.Count) return;

//        RectTransform row = trackRows[trackIndex];
//        foreach (Transform child in row)
//        {
//            if (child.name != "CloseEmpty")
//            {
//                child.gameObject.SetActive(isVisible);
//            }
//        }
//    }

//    public void SetTrackLock(int trackIndex, bool isLocked)
//    {
//        if (trackIndex < 0 || trackIndex >= trackLockedStates.Count) return;
//        trackLockedStates[trackIndex] = isLocked;
//    }

//    public List<VideoController> GetVideoControllers() => videoControllerList;
//    public List<ImageController> GetImageControllers() => imageControllerList;
//    public List<AudioController> GetAudioControllers() => audioControllerList;
//}