using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TimelineRulerManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private RectTransform timeRulerRT;
    private List<GameObject> timeMarkers = new List<GameObject>();
    private Font timeFont;


    //[Header("Event Marker")]
    //[SerializeField] private GameObject eventMarkerPrefab;
    private List<GameObject> spawnedEventMarkers = new List<GameObject>();

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }


    public void CreateTimeRuler(RectTransform scrollViewContent, TimelineConfig config, RectTransform timeRulerParent)
    {
        GameObject rulerGO = new GameObject("TimeRuler", typeof(RectTransform), typeof(Image));
        timeRulerRT = rulerGO.GetComponent<RectTransform>();
        timeRulerRT.SetParent(timeRulerParent, false);

        timeRulerRT.anchorMin = new Vector2(0, 1);
        timeRulerRT.anchorMax = new Vector2(0, 1);
        timeRulerRT.pivot = new Vector2(0, 0);
        timeRulerRT.anchoredPosition = new Vector2(0, 0);
        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeline.GetContentWidth());
        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, config.rulerHeight);

        Image bg = rulerGO.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        bg.raycastTarget = true;

        timeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var tracksRoot = timeline.GetTrackManager().GetTracksRoot();
        if (tracksRoot)
        {
            tracksRoot.anchoredPosition = new Vector2(0, -config.rulerHeight);
        }
        var eventTrigger = rulerGO.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => { OnRulerClick((PointerEventData)data); });
        eventTrigger.triggers.Add(entry);
    }

    private void OnRulerClick(PointerEventData eventData)
    {
        GameObject eventMarkerPrefab = timeline.eventMarkerPrefab;

        if (timeline == null || eventMarkerPrefab == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            timeRulerRT,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        float clickedTime = timeline.XToTime(localPoint.x);

        var playbackManager = timeline.GetComponent<TimelinePlaybackManager>();
        if (playbackManager != null)
        {
            TimelineEvent newEvent = playbackManager.AddEvent(clickedTime);

            GameObject markerInstance = Instantiate(eventMarkerPrefab, timeRulerRT);
            RectTransform markerRT = markerInstance.GetComponent<RectTransform>();

            markerRT.anchorMin = new Vector2(0, 1);
            markerRT.anchorMax = new Vector2(0, 1);
            markerRT.pivot = new Vector2(0.5f, 1);
            markerRT.anchoredPosition = new Vector2(timeline.TimeToX(clickedTime), 0);
            markerRT.localScale = Vector3.one*0.08f;

            var markerScript = markerInstance.GetComponent<TimelineEventMarker>();
            if (markerScript != null)
            {
                markerScript.Initialize(newEvent);
            }

            spawnedEventMarkers.Add(markerInstance);
        }
    }

    public void ClearEventMarkers()
    {
        foreach (var marker in spawnedEventMarkers)
        {
            Destroy(marker);
        }
        spawnedEventMarkers.Clear();
    }
    public void UpdateTimeRuler(TimelineConfig config)
    {
        if (!timeRulerRT) return;

        foreach (var marker in timeMarkers)
        {
            if (marker) Destroy(marker);
        }
        timeMarkers.Clear();

        timeRulerRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeline.GetContentWidth());

        float pixelsPerSecond = config.gridCellHorizontalPixelCount / 4f;
        float targetPixelInterval = 80f;
        float secondsPerInterval = targetPixelInterval / pixelsPerSecond;

        float[] niceIntervals = { 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f, 15f, 30f, 60f };
        float bestInterval = 60f;
        foreach (float interval in niceIntervals)
        {
            if (secondsPerInterval < interval)
            {
                bestInterval = interval;
                break;
            }
        }

        float contentWidth = timeline.GetContentWidth();
        float maxTime = timeline.XToTime(contentWidth);
        int subDivisions = 5;

        for (float time = 0; time <= maxTime; time += bestInterval)
        {
            CreateTimeMarker(time, true, config);

            for (int i = 1; i < subDivisions; i++)
            {
                float minorTime = time + (bestInterval / subDivisions) * i;
                if (minorTime <= maxTime)
                {
                    CreateTimeMarker(minorTime, false, config);
                }
            }
        }
    }

    private void CreateTimeMarker(float timeSeconds, bool isMajorTick, TimelineConfig config)
    {
        float xPos = timeline.TimeToX(timeSeconds);

        GameObject tickGO = new GameObject($"Tick_{timeSeconds:F1}s", typeof(RectTransform), typeof(Image));
        RectTransform tickRT = tickGO.GetComponent<RectTransform>();
        tickRT.SetParent(timeRulerRT, false);
        tickRT.anchorMin = new Vector2(0, 0);
        tickRT.anchorMax = new Vector2(0, 1);
        tickRT.pivot = new Vector2(0.5f, 0);
        tickRT.anchoredPosition = new Vector2(xPos, 0);

        float tickWidth = isMajorTick ? 1f : 1f;
        float tickHeightFactor = isMajorTick ? 0.8f : 0.3f;

        tickRT.sizeDelta = new Vector2(tickWidth, config.rulerHeight * tickHeightFactor);

        Image tickImg = tickGO.GetComponent<Image>();
        tickImg.color = isMajorTick ? config.majorTickColor : config.minorTickColor;
        tickImg.raycastTarget = false;
        timeMarkers.Add(tickGO);

        if (isMajorTick)
        {
            CreateTimeText(timeSeconds, xPos, config);
        }
    }

    private void CreateTimeText(float timeSeconds, float xPos, TimelineConfig config)
    {
        GameObject textGO = new GameObject($"TimeText_{timeSeconds:F1}s", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(timeRulerRT, false);

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = FormatTime(timeSeconds);
        text.fontSize = 12;
        text.color = config.timeTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableKerning = false;

        float preferredWidth = text.preferredWidth;

        textRT.sizeDelta = new Vector2(preferredWidth + 4f, 20f);

        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(0, 1);
        textRT.pivot = new Vector2(0.5f, 0);
        textRT.anchoredPosition = new Vector2(xPos, 20f);

        timeMarkers.Add(textGO);
    }

    private string FormatTime(float timeInSeconds)
    {
        timeInSeconds = Mathf.Max(0, timeInSeconds);
        int hours = Mathf.FloorToInt(timeInSeconds / 3600);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100) % 100);

        if (hours > 0)
        {
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", hours, minutes, seconds, milliseconds);
        }
        else
        {
            return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        }
    }
}