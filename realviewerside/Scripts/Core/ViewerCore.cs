using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ViewerCore : MonoBehaviour
{
    [Header("Display")]
    public RectTransform displayArea;

    [Header("Controls")]
    public Button playButton;
    public Button pauseButton;
    public Slider timeSlider;
    public Text timeText; //this is not needed anymore but I am keeping it just in case

    private TimelinePlaybackManager playbackManager;
    private TimelineWindowManager windowManager;
    private Timer timer;
    private TimelineXMLSerializer xmlSerializer;

    // To understand if the user is currently dragging the slider
    private bool isUserDraggingSlider = false;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
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
    }

    void Update()
    {
        if (playbackManager != null && playbackManager.IsPlaying && !isUserDraggingSlider && timeSlider != null)
        {
            timeSlider.value = playbackManager.GetTime();
        }
    }

    void InitializeComponents()
    {
        timer = GetComponent<Timer>() ?? gameObject.AddComponent<Timer>();
        playbackManager = GetComponent<TimelinePlaybackManager>() ?? gameObject.AddComponent<TimelinePlaybackManager>();
        windowManager = GetComponent<TimelineWindowManager>() ?? gameObject.AddComponent<TimelineWindowManager>();
        xmlSerializer = GetComponent<TimelineXMLSerializer>() ?? gameObject.AddComponent<TimelineXMLSerializer>();

        playbackManager.Initialize(gameObject);
        windowManager.Initialize(displayArea);
    }

    public void LoadProject(string xmlPath)
    {
        StartCoroutine(LoadAndSetupSlider(xmlPath));
    }

    private System.Collections.IEnumerator LoadAndSetupSlider(string xmlPath)
    {
        xmlSerializer.ImportFromXML(xmlPath);

        yield return new WaitForSeconds(1f); // This can be changed to a more robust check if needed

        SetupSlider();
    }

    private void SetupSlider()
    {
        if (timeSlider == null || playbackManager == null) return;

        // Check the project's total time
        float maxTime = 0f;
        maxTime = Mathf.Max(maxTime, playbackManager.videoControllers.Any() ? playbackManager.videoControllers.Max(c => c.getvc().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.imageControllers.Any() ? playbackManager.imageControllers.Max(c => c.getic().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.audioControllers.Any() ? playbackManager.audioControllers.Max(c => c.getac().getEnd()) : 0);
        maxTime = Mathf.Max(maxTime, playbackManager.modelControllers.Any() ? playbackManager.modelControllers.Max(c => c.getmc().getEnd()) : 0);

        timeSlider.minValue = 0;
        timeSlider.maxValue = maxTime;
        timeSlider.value = 0;
        Debug.Log($"Slider ayarlandı: Toplam süre = {maxTime:F2} saniye");
    }

    public void OnSliderValueChanged(float value)
    {
        if (isUserDraggingSlider)
        {
            SetTime(value);
        }
    }

    // Oynatma kontrolleri
    public void Play() => playbackManager.Play();
    public void Pause() => playbackManager.Stop();
    public void SetTime(float time)
    {
        playbackManager.SetTime(time);
        if (!isUserDraggingSlider && timeSlider != null)
        {
            timeSlider.value = time;
        }
    }
}