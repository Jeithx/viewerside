using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimelineSelectionManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private RectTransform selectionOverlayRT;
    private RectTransform marqueeRT;
    private bool selecting;
    private Vector2 selectStartScreen;
    private bool additiveSelect;

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }

    public void EnsureSelectionOverlay(RectTransform scrollViewContent)
    {
        var go = new GameObject("SelectionOverlay", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        selectionOverlayRT = go.GetComponent<RectTransform>();
        selectionOverlayRT.SetParent(scrollViewContent, false);

        selectionOverlayRT.anchorMin = new Vector2(0, 1);
        selectionOverlayRT.anchorMax = new Vector2(0, 1);
        selectionOverlayRT.pivot = new Vector2(0, 1);
        selectionOverlayRT.anchoredPosition = Vector2.zero;
        selectionOverlayRT.sizeDelta = scrollViewContent.sizeDelta;

        var le = go.GetComponent<LayoutElement>();
        le.ignoreLayout = true;

        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }

    public void EnsureMarquee()
    {
        var go = new GameObject("Marquee", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        marqueeRT = go.GetComponent<RectTransform>();
        marqueeRT.SetParent(selectionOverlayRT, false);
        marqueeRT.anchorMin = marqueeRT.anchorMax = new Vector2(0, 1);
        marqueeRT.pivot = new Vector2(0, 1);
        marqueeRT.sizeDelta = Vector2.zero;
        marqueeRT.anchoredPosition = Vector2.zero;

        var le = go.GetComponent<LayoutElement>();
        le.ignoreLayout = true;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        img.raycastTarget = false;
        marqueeRT.gameObject.SetActive(false);
    }

    public void OnBackgroundPointerDown(BaseEventData bed)
    {
        var ped = (PointerEventData)bed;
        var barManager = timeline.GetBarManager();

        if (barManager.FindBarRT(ped)) return;

        additiveSelect = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                           Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        selecting = true;
        selectStartScreen = ped.position;

        if (!additiveSelect) barManager.ClearSelection();

        ShowMarquee(selectStartScreen, selectStartScreen);
    }

    public void OnBackgroundDrag(BaseEventData bed)
    {
        if (!selecting) return;
        var ped = (PointerEventData)bed;
        ShowMarquee(selectStartScreen, ped.position);
        UpdateSelectionFromMarquee(additiveSelect);
    }

    public void OnBackgroundPointerUp(BaseEventData bed)
    {
        if (!selecting) return;
        selecting = false;
        marqueeRT.gameObject.SetActive(false);
    }

    private void ShowMarquee(Vector2 startScreen, Vector2 endScreen)
    {
        timeline.ScreenToLocal(selectionOverlayRT, startScreen, out var a);
        timeline.ScreenToLocal(selectionOverlayRT, endScreen, out var b);

        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minY = Mathf.Min(a.y, b.y);
        float maxY = Mathf.Max(a.y, b.y);

        marqueeRT.gameObject.SetActive(true);
        marqueeRT.anchoredPosition = new Vector2(minX, maxY);
        marqueeRT.sizeDelta = new Vector2(maxX - minX, maxY - minY);

        selectionOverlayRT.SetAsLastSibling();
        marqueeRT.SetAsLastSibling();
    }

    private void UpdateSelectionFromMarquee(bool additive)
    {
        var barManager = timeline.GetBarManager();
        if (!additive) barManager.ClearSelection();

        var marCorners = new Vector3[4];
        marqueeRT.GetWorldCorners(marCorners);
        Rect marRect = new Rect(marCorners[0], marCorners[2] - marCorners[0]);

        foreach (var kv in barManager.GetBarMap())
        {
            var rt = kv.Key;
            if (!rt || !rt.gameObject.activeInHierarchy) continue;

            var bc = new Vector3[4];
            rt.GetWorldCorners(bc);
            Rect barRect = new Rect(bc[0], bc[2] - bc[0]);

            if (barRect.Overlaps(marRect)) barManager.SelectBar(rt, true);
        }
    }

    public bool IsSelecting() => selecting;
}