using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ViewerCore : MonoBehaviour
{
    [Header("Display")]
    public RectTransform displayArea;

    [Header("Controls")]
    public Button playButton;
    public Button pauseButton;
    public Slider timeSlider;
    public Text timeText;

    [Header("Prefabs")]
    public GameObject videoPrefab;

    private TimelinePlaybackManager playbackManager;
    private TimelineWindowManager windowManager;
    private Timer timer;
    private TimelineXMLSerializer xmlSerializer;

    void Awake()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        timer = GetComponent<Timer>() ?? gameObject.AddComponent<Timer>();
        playbackManager = GetComponent<TimelinePlaybackManager>() ?? gameObject.AddComponent<TimelinePlaybackManager>();
        windowManager = GetComponent<TimelineWindowManager>() ?? gameObject.AddComponent<TimelineWindowManager>();
        xmlSerializer = GetComponent<TimelineXMLSerializer>() ?? gameObject.AddComponent<TimelineXMLSerializer>();

        playbackManager.Initialize(gameObject);
        windowManager.Initialize(null, displayArea);
    }

    public void LoadProject(string xmlPath)
    {
        xmlSerializer.ImportFromXML(xmlPath);
    }

    public void Play() => playbackManager.Play();
    public void Pause() => playbackManager.Stop();
    public void SetTime(float time) => playbackManager.SetTime(time);
}