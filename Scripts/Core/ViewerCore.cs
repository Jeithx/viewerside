using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using Scripts.Utility;
using System.Collections;


public class ViewerCore : BasicSingleton<ViewerCore>
{
    [Header("Display")]
    public RectTransform displayArea;

    [Header("Controls")]
    public Button playButton;
    //public Button pauseButton;
    public Button restartButton;
    public Button muteButton;
    public Slider timeSlider;
    public Button Forwards15Button;
    public Button Backwards15Button;
    public Button textButton;
    public Button fulscreenButton;
    public Text timeText; //this is not needed anymore but I am keeping it just in case
 

    private TimelinePlaybackManager playbackManager;
    private TimelineWindowManager windowManager;
    private Timer timer;
    private TimelineXMLSerializer xmlSerializer;
    private TimelineXMLUI xmlUi;

    [Header("Play/Pause Icons")]
    public Image playPauseButtonIcon;
    public Sprite playIcon;
    public Sprite pauseIcon;

    [Header("Volume Control")]
    public GameObject volumePanel;
    public Slider volumeSlider;
    public Image muteButtonIcon;
    public Sprite lowIcon;
    public Sprite muteIcon;
    public Sprite highIcon;
    public GameObject backgroundPanel;
    public Button backgroundPanelButton;

    [Header("Description Panel")]
    public GameObject descriptionPanel;
    public LayoutElement panelContainerLayoutElement;
    private RectTransform descriptionPanelRect;
    [SerializeField]private float panelFullHeight = 120f;
    private bool isPanelActive = false;
    private bool panelPositionsInitialized = false;
    private Vector2 panelVisiblePosition = Vector2.zero;
    private Vector2 panelHiddenPosition;
    public TextMeshProUGUI descriptionText;


    private bool volumePanelVisible = false;
    private bool isCleanedUp = false;



    private bool isMuted = false;

    // To understand if the user is currently dragging the slider
    private bool isUserDraggingSlider = false;
    private bool isVolumeSliderBeingDragged = false;

    private float cachedTotalTime = -1f;

    void Awake()
    {
        base.Awake();
        InitializeComponents();
    }

    void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Time Slider Setup
        if (timeSlider != null)
        {
            timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            var eventTrigger = timeSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isUserDraggingSlider = true; });
            eventTrigger.triggers.Add(pointerDown);

            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isUserDraggingSlider = false; });
            eventTrigger.triggers.Add(pointerUp);
        }

        if (Forwards15Button != null && Backwards15Button != null)
        {
            Forwards15Button.onClick.AddListener(SkipForward15);
            Backwards15Button.onClick.AddListener(SkipBack15);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartPlayback);
        }

        if (muteButton != null)
        {
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(ToggleVolumePanel);
        }

        if (textButton != null)
        {
            textButton.onClick.RemoveAllListeners();
            textButton.onClick.AddListener(OnTextButtonClicked);
        }

        if (backgroundPanelButton != null) backgroundPanelButton.onClick.AddListener(DisableVolumeSlider);

        if (fulscreenButton != null) fulscreenButton.onClick.AddListener(DisableVolumeSlider);



        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                Debug.Log("play button clicked");
                TogglePlayPause();
                UpdatePlayButtonIcon();
            });
        }
        if (volumePanel != null) volumePanel.SetActive(false);
        if (volumeSlider != null)
        {

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 100f;
            volumeSlider.value = AudioListener.volume * 100f;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

            var eventTrigger = volumeSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isVolumeSliderBeingDragged = true; });
            eventTrigger.triggers.Add(pointerDown);

            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isVolumeSliderBeingDragged = false; });
            eventTrigger.triggers.Add(pointerUp);
        }
        //descriptionText.text = descriptionPanel.GetComponentInChildren<TextMeshProUGUI>().text;
        UpdateVolumeIcon();
        //DisableButtons();
        StartCoroutine(InitializePanelPositions());
    }
    private IEnumerator InitializePanelPositions()
    {
        if (descriptionPanel == null || panelContainerLayoutElement == null)
            yield break;

        yield return new WaitForEndOfFrame();

        descriptionPanelRect = descriptionPanel.GetComponent<RectTransform>();

        var parentRect = descriptionPanelRect.parent.GetComponent<RectTransform>();
        float parentWidth = parentRect.rect.width;


  
        panelHiddenPosition = new Vector2(parentWidth, 0); // Bu değişkeni offset'leri ayarlamak için kullanacağız.

   
        descriptionPanelRect.offsetMin = new Vector2(panelHiddenPosition.x, descriptionPanelRect.offsetMin.y);
        descriptionPanelRect.offsetMax = new Vector2(panelHiddenPosition.x, descriptionPanelRect.offsetMax.y);

        panelContainerLayoutElement.preferredHeight = 0;
        descriptionPanel.GetComponent<CanvasGroup>().alpha = 0;

        panelPositionsInitialized = true;

        Debug.Log("Panel gizlendi. Yeni Left offset: " + descriptionPanelRect.offsetMin.x);
    }

    private void ToggleVolumePanel()
    {
        if (volumePanel == null) return;

        if (isVolumeSliderBeingDragged) return;

        volumePanelVisible = !volumePanelVisible;
        volumePanel.SetActive(volumePanelVisible);
        backgroundPanel.SetActive(volumePanelVisible);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (isVolumeSliderBeingDragged) return;

        if (volumePanelVisible)
        {
            DisableVolumeSlider();
        }
    }

    private void OnTextButtonClicked()
    {
        if (volumePanelVisible)
        {
            DisableVolumeSlider();
        }
        else
        {
            TriggerPanel();
        }
    }




    private void UpdateVolumeIcon()
    {
        if (muteButtonIcon == null) return;

        float v = AudioListener.volume * 100f;
        if (v <= 0f)
        {
            // 0 => mute icon
            muteButtonIcon.sprite = muteIcon;
        }
        else if (v > 50f)
        {
            // >50 => highIcon
            muteButtonIcon.sprite = highIcon;
        }
        else
        {
            // 0–50 => lowIcon
            muteButtonIcon.sprite = lowIcon;
        }
    }


    public void RestartPlayback()
    {
        Debug.Log("Restart button clicked - Resetting timeline to beginning");

        Pause();

        SetTime(0f);

        if (timer != null)
        {
            timer.SetCurrentTime(0f);
        }

        if (timeSlider != null)
        {
            timeSlider.value = 0f;
        }

        ResetAllControllers();

        Debug.Log("Timeline restarted successfully");
    }

    private void ResetAllControllers()
    {
        // Video controllers reset
        foreach (var controller in playbackManager.videoControllers)
        {
            if (controller != null)
            {
                controller.ResetState();
                controller.ScrubTo(0f, false);
            }
        }

        // Image controllers reset
        foreach (var controller in playbackManager.imageControllers)
        {
            if (controller != null)
            {
                controller.ResetState();
                controller.ScrubTo(0f, false);
            }
        }

        // Audio controllers reset
        foreach (var controller in playbackManager.audioControllers)
        {
            if (controller != null)
            {
                controller.ResetState();
                controller.ScrubTo(0f, false);
            }
        }

        // Model controllers reset
        foreach (var controller in playbackManager.modelControllers)
        {
            if (controller != null)
            {
                controller.ResetState();
                controller.ScrubTo(0f, false);
            }
        }
    }

    private void UpdatePlayButtonIcon()
    {
        if (playPauseButtonIcon != null)
        {
            if (playbackManager != null && playbackManager.IsPlaying)
            {
                playPauseButtonIcon.sprite = pauseIcon;
            }
            else
            {
                playPauseButtonIcon.sprite = playIcon;
            }
        }
    }
    //public void ToggleMute()
    //{
    //    isMuted = !isMuted;

    //    if (isMuted)
    //    {
    //        // Sesi kapat
    //        previousVolume = AudioListener.volume; // Mevcut ses seviyesini kaydet
    //        AudioListener.volume = 0f;
    //        Debug.Log("Audio muted");
    //    }
    //    else
    //    {
    //        // Sesi aç
    //        AudioListener.volume = previousVolume;
    //        Debug.Log($"Audio unmuted - Volume restored to: {previousVolume}");
    //    }

    //    UpdateMuteButtonIcon();
    //}

    void Update()
    {
        if (playbackManager != null && playbackManager.IsPlaying && !isUserDraggingSlider && timeSlider != null)
        {
            float currentTime = playbackManager.GetTime();
            timeSlider.value = currentTime;

            if (cachedTotalTime > 0f && currentTime >= cachedTotalTime)
            {
                Debug.Log($"Timeline tamamlandı! Current: {currentTime:F2}s, Total: {cachedTotalTime:F2}s - Durduruluyor...");

                Pause();

                UpdatePlayButtonIcon();

                timeSlider.value = cachedTotalTime;
            }
        }
    }

    void InitializeComponents()
    {
        timer = GetComponent<Timer>() ?? gameObject.AddComponent<Timer>();
        playbackManager = GetComponent<TimelinePlaybackManager>() ?? gameObject.AddComponent<TimelinePlaybackManager>();
        windowManager = GetComponent<TimelineWindowManager>() ?? gameObject.AddComponent<TimelineWindowManager>();
        xmlSerializer = GetComponent<TimelineXMLSerializer>() ?? gameObject.AddComponent<TimelineXMLSerializer>();
        xmlUi = GetComponent<TimelineXMLUI>() ?? gameObject.AddComponent<TimelineXMLUI>();

        playbackManager.Initialize(gameObject);
        windowManager.Initialize(displayArea);
    }

    public void LoadProject(string xmlPath)
    {
        StartCoroutine(LoadAndSetupSlider(xmlPath));
    }

    private void DisableButtons()
    {
        playButton.interactable = false;
        //restartButton.interactable = false;
        //muteButton.interactable = false;
        timeSlider.interactable = false;
        Forwards15Button.interactable = false;
        Backwards15Button.interactable = false;
        //textButton.interactable = false;
        //fulscreenButton.interactable = false;

    }
    private void EnableButtons()
    {
        playButton.interactable = true;
        //restartButton.interactable = true;
        //muteButton.interactable = true;
        timeSlider.interactable = true;
        Forwards15Button.interactable = true;
        Backwards15Button.interactable = true;
        //textButton.interactable = true;
        //fulscreenButton.interactable = true;
    }
    public void InternalDispose()
        {
            if (isCleanedUp) return;
            DisableButtons();
            xmlSerializer.ClearAll();
            ResetTimerAndUI();

            isCleanedUp = true;
            Debug.Log("[ViewerCore] Internal cleanup tamamlandı.");
    }

    private void ResetTimerAndUI()
    {
        if (timer != null) timer.SetCurrentTime(0f);
        if (timeSlider != null) timeSlider.value = 0f;
        if (playPauseButtonIcon != null) playPauseButtonIcon.sprite = playIcon;
    }

    //private System.Collections.IEnumerator LoadAndSetupSlider(string xmlPath)
    //{
    //    EnableButtons();
    //    isCleanedUp = false;
    //    xmlSerializer.ImportFromXML(xmlPath);

    //    yield return new WaitForSeconds(1f); // This can be changed to a more robust check if needed

    //    GetComponent<PopupEventManager>()?.LoadEventsFromSerializer();

    //    SetupSlider();
    //}

    private System.Collections.IEnumerator LoadAndSetupSlider(string xmlPath)
    {
        EnableButtons();
        isCleanedUp = false;

        InvalidateTotalTimeCache();

        xmlSerializer.ImportFromXML(xmlPath);

        yield return new WaitForSeconds(1f);

        GetComponent<PopupEventManager>()?.LoadEventsFromSerializer();

        SetupSlider();
    }

    private void SetupSlider()
    {
        if (timeSlider == null || playbackManager == null) return;

        cachedTotalTime = GetTotalTime();

        timeSlider.minValue = 0;
        timeSlider.maxValue = cachedTotalTime;
        timeSlider.value = 0;
        Debug.Log($"Slider ayarlandı: Total time = {cachedTotalTime:F2}s");
    }
    public void InvalidateTotalTimeCache()
    {
        cachedTotalTime = -1f;
    }
    private float GetTotalTime()
    {
        // Check the project's total time
        float maxTime = 0f;
        maxTime = Mathf.Max(maxTime, playbackManager.videoControllers.Any() ? playbackManager.videoControllers.Max(c => c.getvc().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.imageControllers.Any() ? playbackManager.imageControllers.Max(c => c.getic().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.audioControllers.Any() ? playbackManager.audioControllers.Max(c => c.getac().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.modelControllers.Any() ? playbackManager.modelControllers.Max(c => c.getmc().getEnd()) : 0);

        return maxTime;
    }

    public void OnSliderValueChanged(float value)
    {
        if (isUserDraggingSlider)
        {
            SetTime(value);
        }
    }

        public void SkipForward15()
    {
        if (playbackManager == null) return;
        float newTime = playbackManager.GetTime() + 15f;
        if (timeSlider != null) newTime = Mathf.Min(newTime, timeSlider.maxValue);
        SetTime(newTime);
    }

    public void SkipBack15()
    {
        if (playbackManager == null) return;
        float newTime = playbackManager.GetTime() - 15f;
        newTime = Mathf.Max(newTime, 0f);
        SetTime(newTime);
    }

    //public void OldTriggerPanel()
    //{
    //    isPanelActive = !isPanelActive;

    //    var layout = descriptionPanel.GetComponent<LayoutElement>();
    //    if (isPanelActive)
    //    {

    //        descriptionText.fontSize = 24;
    //        layout.preferredHeight = 80f;
    //    }
    //    else
    //    {
    //        descriptionText.fontSize = 0;
    //        layout.preferredHeight = 0f;
    //    }

    //    Debug.Log("InfoPanel state: " + (isPanelActive ? "Open" : "Closed"));
    //}

    private void DisableVolumeSlider()
    {
        if (!volumePanelVisible) return;
        ToggleVolumePanel();
        backgroundPanel.SetActive(false);
    }


    public void TriggerPanel()
    {
        if (!panelPositionsInitialized) return;

        descriptionPanelRect.DOKill();
        panelContainerLayoutElement.DOKill();

        isPanelActive = !isPanelActive;

        if (isPanelActive)
        {
          
            panelContainerLayoutElement.preferredHeight = panelFullHeight;

            descriptionPanelRect.DOAnchorPosX(panelVisiblePosition.x, 0.8f).SetEase(Ease.OutQuad);

            //descriptionPanel.GetComponent<CanvasGroup>().DOFade(1, 0.3f).SetDelay(0.2f);
        }
        else
        {

            descriptionPanelRect.DOAnchorPosX(panelHiddenPosition.x*2, 0.8f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    panelContainerLayoutElement.preferredHeight = 0;
                });

            //descriptionPanel.GetComponent<CanvasGroup>().DOFade(0, 0.2f);
        }
    }


    //play controls
    public void Play()
    {
        playbackManager.Play();
        UpdatePlayButtonIcon();
    }
    public void Pause()
    {
        playbackManager.Stop();
        UpdatePlayButtonIcon();
    }

    public void TogglePlayPause()
    {
        if (playbackManager.IsPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }
    public void SetTime(float time)
    {
        playbackManager.SetTime(time);
        if (!isUserDraggingSlider && timeSlider != null)
        {
            timeSlider.value = time;
        }
    }
    

    public bool IsMuted() => isMuted;

    private void OnVolumeSliderChanged(float value)
    {
        float normalizedVolume = value / 100f;
        AudioListener.volume = normalizedVolume;

        UpdateVolumeIcon();

        Debug.Log($"Volume changed to: {value}% (normalized: {normalizedVolume:F2})");
    }

}