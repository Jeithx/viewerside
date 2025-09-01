using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Unity.UI;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Refs")]
    public GameObject timelinePanel;   // assign ScrollView Content (bars parent)
    public TimelineGrid timelineGrid;

    public GameObject timeShowPanel;
    public TextMeshProUGUI timeDisplay;
    public TextMeshProUGUI timerText;

    private RectTransform handleRT;
    private RectTransform parentRT;
    private Canvas canvas;

    private bool isDragging;
    private bool pointerDown;


    void Start()
    {
        Debug.Log("DragHandler initialized on " + this.name);
        canvas = GetComponentInParent<Canvas>();
        handleRT = GetComponent<RectTransform>();
        parentRT = timelinePanel ? timelinePanel.GetComponent<RectTransform>() : null;

        handleRT.anchorMin = new Vector2(0f, 0.5f);
        handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot = new Vector2(0f, 0.5f);

        StartCoroutine(InitNextFrame());
    }

    System.Collections.IEnumerator InitNextFrame()
    {
        yield return null;
        UnityEngine.Canvas.ForceUpdateCanvases();
        if (timelineGrid != null) SetXFromTime(timelineGrid.GetTime());
    }

    void Update()
    {
        if (timelineGrid == null) return;

        if (isDragging && !pointerDown && !Input.GetMouseButton(0))
            isDragging = false;

        if (!isDragging)
            SetXFromTime(timelineGrid.GetTime());
    }

    public void OnPointerDown(PointerEventData e) => pointerDown = true;
    public void OnPointerUp(PointerEventData e) => pointerDown = false;

    public void OnBeginDrag(PointerEventData eventData) { isDragging = true;
        timeShowPanel.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (timelineGrid == null || handleRT == null || parentRT == null) return;

        float scale = (canvas ? canvas.scaleFactor : 1f);
        float deltaX = eventData.delta.x / Mathf.Max(0.0001f, scale);

        float newX = handleRT.anchoredPosition.x + deltaX;

        float maxX = Mathf.Max(0f, parentRT.rect.width - handleRT.rect.width);
        newX = Mathf.Clamp(newX, 0f, maxX);

        handleRT.anchoredPosition = new Vector2(newX, handleRT.anchoredPosition.y);

        float t = timelineGrid.XToTime(newX);
        timelineGrid.SetTime(t);

        UpdateTimeDisplay(t);

    }

    public void OnEndDrag(PointerEventData eventData) { isDragging = false;
        timeShowPanel.gameObject.SetActive(false);
    }

    private void UpdateTimeDisplay(float timeInSeconds)
    {
        if (!timeDisplay) return;
        if (!timerText) return;

        timeDisplay.text = timerText.text;
    }

    private void SetXFromTime(float timeSeconds)
    {
        if (parentRT == null) return;

        float x = timelineGrid.TimeToX(timeSeconds);
        float maxX = Mathf.Max(0f, parentRT.rect.width - handleRT.rect.width);
        x = Mathf.Clamp(x, 0f, maxX);

        handleRT.anchoredPosition = new Vector2(x, handleRT.anchoredPosition.y);
    }
}
