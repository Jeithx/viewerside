using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimelineBarManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private readonly Dictionary<RectTransform, Content> barMap = new();
    private readonly Dictionary<Content, RectTransform> contentToBar = new();

    private RectTransform draggingBar;
    private RectTransform resizingBar;
    private bool resizingLeft;
    private float barGrabOffsetX;

    private readonly HashSet<RectTransform> selectedBars = new();
    private readonly Dictionary<RectTransform, Vector2> groupStartPos = new();
    private bool draggingGroup;

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }

    public void RegisterBar(RectTransform barRT, Content content, bool isImage)
    {
        if (!barRT || content == null) return;

        if (!(barRT.parent is RectTransform parentRow) || timeline.GetRowIndexOf(parentRow) < 0)
        {
            timeline.EnsureRowsAtLeast(1);
            var trackRows = timeline.GetTrackRows();
            barRT.SetParent(trackRows[^1], false);
            barRT.localScale = Vector3.one;
        }
        barRT.anchoredPosition = new Vector2(barRT.anchoredPosition.x, 0f);

        barMap[barRT] = content;
        contentToBar[content] = barRT;

        int rowIdx = timeline.GetRowIndexOf(barRT.parent as RectTransform);
        content.SetLayer(timeline.LayerFromRow(rowIdx));
        timeline.UpdateWindowOrders();

        var img = barRT.GetComponent<Image>() ?? barRT.gameObject.AddComponent<Image>();
        img.raycastTarget = true;

        barRT.SetAsLastSibling();

        var trig = barRT.gameObject.GetComponent<EventTrigger>() ?? barRT.gameObject.AddComponent<EventTrigger>();
        AddEntry(trig, EventTriggerType.PointerDown, OnBarPointerDown);
        AddEntry(trig, EventTriggerType.BeginDrag, OnBarBeginDrag);
        AddEntry(trig, EventTriggerType.Drag, OnBarDrag);
        AddEntry(trig, EventTriggerType.EndDrag, OnBarEndDrag);

        if (isImage)
        {
            AddBarResizeHandle(barRT, content, true);
            AddBarResizeHandle(barRT, content, false);
        }

        timeline.EnsureContentWidthForBar(barRT);
        var currentParentRow = barRT.parent as RectTransform;
        currentParentRow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeline.GetContentWidth());
        //timeline.UpdateRowCloseButtons();

        if (timeline.GetConfig().enablePreviews)
        {
            timeline.CreateBarPreviews(barRT, content);
        }

        int rowIndex = timeline.GetRowIndexOf(currentParentRow);
        var trackHeaders = timeline.GetTrackHeaders();

        if (rowIndex != -1 && rowIndex < trackHeaders.Count && currentParentRow.childCount <= 2)
        {
            string contentName = "Content";
            if (content is VideoContent vc)
            {
                contentName = System.IO.Path.GetFileNameWithoutExtension(vc.getPath());
            }
            else if (content is ImageContent ic)
            {
                contentName = System.IO.Path.GetFileNameWithoutExtension(ic.getPath());
            }
            else if (content is AudioContent ac)
            {
                contentName = System.IO.Path.GetFileNameWithoutExtension(ac.getPath());
            }
            else if (content is ModelContent mc)
            {
                var modelPrefabField = typeof(ModelContent).GetField("_modelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (modelPrefabField != null)
                {
                    GameObject prefab = modelPrefabField.GetValue(mc) as GameObject;
                    if (prefab != null)
                    {
                        contentName = prefab.name;
                    }
                    else
                    {
                        contentName = "3D Model";
                    }
                }
            }

            //trackHeaders[rowIndex].SetTrackName(contentName);
        }
    }

    private void AddEntry(EventTrigger trig, EventTriggerType t, System.Action<BaseEventData> cb)
    {
        var e = new EventTrigger.Entry { eventID = t };
        e.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
        trig.triggers.Add(e);
    }

    private void OnBarPointerDown(BaseEventData bed)
    {
        var ped = (PointerEventData)bed;
        GameObject go = ped.pointerPress ?? ped.pointerEnter;
        RectTransform rt = FindBarRT(go);

        Debug.Log($"Bar clicked: {go?.name}, RT found: {rt?.name}");

        if (!rt) return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                      Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        if (!ctrl && !selectedBars.Contains(rt) && !timeline.IsSelecting())
        {
            ClearSelection();
            SelectBar(rt, true);
        }
        else if (ctrl)
        {
            if (selectedBars.Contains(rt)) SelectBar(rt, false);
            else SelectBar(rt, true);
        }

        var inspectorPanel = timeline.GetInspectorPanel();
        if (inspectorPanel != null && barMap.TryGetValue(rt, out Content clickedContent))
        {
            inspectorPanel.Show(clickedContent);
        }
    }

    private RectTransform FindBarRT(GameObject go)
    {
        RectTransform rt = null;
        if (go != null)
        {
            var t = go.transform;
            while (t != null)
            {
                rt = t as RectTransform;
                if (rt && barMap.ContainsKey(rt)) break;
                t = t.parent; rt = null;
            }
        }
        return rt;
    }

    private void OnBarBeginDrag(BaseEventData bed)
    {
        var ped = (PointerEventData)bed;
        draggingBar = FindBarRT(ped.pointerPress ?? ped.pointerEnter);

        Debug.Log($"Begin drag for bar: {draggingBar?.name}");

        if (!draggingBar) return;

        int trackIndex = timeline.GetRowIndexOf(draggingBar.parent as RectTransform);
        var trackLockedStates = timeline.GetTrackLockedStates();

        if (trackIndex != -1 && trackIndex < trackLockedStates.Count && trackLockedStates[trackIndex])
        {
            draggingBar = null;
            return;
        }

        Vector2 currentPos = draggingBar.anchoredPosition;
        Debug.Log($"Current position: {currentPos}");

        RectTransform parent = draggingBar.parent as RectTransform;
        timeline.ScreenToLocal(parent, ped.position, out var lp);
        barGrabOffsetX = lp.x - draggingBar.anchoredPosition.x;

        draggingGroup = selectedBars.Contains(draggingBar) && selectedBars.Count > 1;
        groupStartPos.Clear();
        if (draggingGroup)
        {
            foreach (var rt in selectedBars)
                if (rt) groupStartPos[rt] = rt.anchoredPosition;
        }
        else
        {
            if (!selectedBars.Contains(draggingBar))
            {
                ClearSelection();
                SelectBar(draggingBar, true);
            }
        }
    }

    private void OnBarDrag(BaseEventData bed)
    {
        if (!draggingBar) return;
        var ped = (PointerEventData)bed;

        RectTransform parentRT = draggingBar.parent as RectTransform;
        timeline.ScreenToLocal(parentRT, ped.position, out var lp);
        float desiredX = lp.x - barGrabOffsetX;

        float maxX = Mathf.Max(0f, parentRT.rect.width - draggingBar.rect.width);
        desiredX = Mathf.Clamp(desiredX, 0f, maxX);

        if (draggingGroup)
        {
            float dx = desiredX - draggingBar.anchoredPosition.x;
            foreach (var rt in selectedBars)
            {
                if (!rt) continue;
                var row = (RectTransform)rt.parent;
                float rowMax = Mathf.Max(0f, row.rect.width - rt.rect.width);
                float newX = Mathf.Clamp(rt.anchoredPosition.x + dx, 0f, rowMax);
                rt.anchoredPosition = new Vector2(newX, 0f);
            }
        }
        else
        {
            draggingBar.anchoredPosition = new Vector2(desiredX, 0f);
            timeline.MaybeMoveBarToRowUnderPointer(ped.position, draggingBar);
        }
    }

    private void OnBarEndDrag(BaseEventData bed)
    {
        if (!draggingBar) return;

        if (draggingGroup)
        {
            foreach (var rt in selectedBars)
            {
                var content = barMap[rt];
                RectTransform row = (RectTransform)rt.parent;

                float startT = timeline.XToTime(rt.anchoredPosition.x);
                float duration = timeline.GetContentDuration(content);

                float snapped = timeline.ComputeSnappedTime(startT, content);
                float resolved = timeline.ResolveNoOverlapStart(row, content, snapped, duration);

                content.SetStart(resolved);
                rt.anchoredPosition = new Vector2(timeline.TimeToX(resolved), 0f);

                timeline.EnsureWidthForTime(resolved + duration);
            }
        }
        else
        {
            var content = barMap[draggingBar];
            var parentRT = (RectTransform)draggingBar.parent;

            float startT = timeline.XToTime(draggingBar.anchoredPosition.x);
            float duration = timeline.GetContentDuration(content);

            float snapped = timeline.ComputeSnappedTime(startT, content);
            float resolved = timeline.ResolveNoOverlapStart(parentRT, content, snapped, duration);

            content.SetStart(resolved);
            draggingBar.anchoredPosition = new Vector2(timeline.TimeToX(resolved), 0f);

            timeline.EnsureWidthForTime(resolved + duration);
        }

        timeline.SetTime(timeline.GetTime());

        draggingBar = null;
        draggingGroup = false;
    }

    private void AddBarResizeHandle(RectTransform barRT, Content content, bool left)
    {
        var go = new GameObject(left ? "ResizeL" : "ResizeR",
            typeof(RectTransform), typeof(Image), typeof(EventTrigger));
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        var et = go.GetComponent<EventTrigger>();

        go.transform.SetParent(barRT, false);
        const float gripW = 10f;

        if (left)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(gripW, 0);
            rt.anchoredPosition = new Vector2(gripW * 0.5f, 0);
        }
        else
        {
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(gripW, 0);
            rt.anchoredPosition = new Vector2(-gripW * 0.5f, 0);
        }

        img.color = new Color(1, 1, 1, 0.001f);
        img.raycastTarget = true;

        var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        begin.callback.AddListener(_ => { resizingBar = barRT; resizingLeft = left; });
        et.triggers.Add(begin);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(ev => OnBarResizeDrag((PointerEventData)ev, content));
        et.triggers.Add(drag);

        var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        end.callback.AddListener(_ => { OnBarResizeEnd(content); resizingBar = null; });
        et.triggers.Add(end);
    }

    private void OnBarResizeDrag(PointerEventData ped, Content content)
    {
        if (!resizingBar) return;

        var parentRT = (RectTransform)resizingBar.parent;
        float scale = timeline.GetCanvasScale();
        float dx = ped.delta.x / Mathf.Max(0.0001f, scale);

        float minW = (timeline.GetConfig().minImageDurationSeconds / 4f) * timeline.GetConfig().gridCellHorizontalPixelCount;

        float newWidth = resizingBar.rect.width + (resizingLeft ? -dx : dx);
        newWidth = Mathf.Max(minW, newWidth);

        if (resizingLeft)
        {
            float newX = Mathf.Clamp(resizingBar.anchoredPosition.x + dx, 0f,
                                      Mathf.Max(0f, parentRT.rect.width - newWidth));
            resizingBar.anchoredPosition = new Vector2(newX, 0f);
        }

        resizingBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

    private void OnBarResizeEnd(Content content)
    {
        if (!resizingBar) return;

        float x = resizingBar.anchoredPosition.x;
        float w = resizingBar.rect.width;

        float startT = timeline.XToTime(x);
        float durT = timeline.XToTime(w) - timeline.XToTime(0f);

        var config = timeline.GetConfig();

        if (resizingLeft)
        {
            float snappedStart = timeline.ComputeSnappedTime(startT, content);
            content.SetStart(snappedStart);

            float endT = startT + durT;
            durT = Mathf.Max(config.minImageDurationSeconds, endT - snappedStart);

            resizingBar.anchoredPosition = new Vector2(timeline.TimeToX(snappedStart), 0f);
            resizingBar.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, timeline.TimeToX(durT) - timeline.TimeToX(0f));
        }
        else
        {
            float endT = startT + durT;
            float snappedEnd = timeline.ComputeSnappedTime(endT, content);
            durT = Mathf.Max(config.minImageDurationSeconds, snappedEnd - content.getStart());

            resizingBar.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, timeline.TimeToX(durT) - timeline.TimeToX(0f));
        }

        timeline.SetImageLength(content, durT);

        timeline.EnsureContentWidthForBar(resizingBar);
        timeline.EnsureWidthForTime(content.getStart() + durT);

        timeline.SetTime(timeline.GetTime());
    }

    public void SelectBar(RectTransform rt, bool selected)
    {
        if (!rt) return;
        var img = rt.GetComponent<Image>();
        var outline = rt.GetComponent<Outline>();
        if (selected)
        {
            selectedBars.Add(rt);
            if (!outline) outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        }
        else
        {
            selectedBars.Remove(rt);
            if (outline) Destroy(outline);
        }
    }

    public void ClearSelection()
    {
        foreach (var rt in new List<RectTransform>(selectedBars))
            SelectBar(rt, false);
        selectedBars.Clear();
    }

    public bool HasBar(RectTransform rt) => barMap.ContainsKey(rt);
    public Content GetContent(RectTransform rt) => barMap.TryGetValue(rt, out var content) ? content : null;
    public RectTransform GetBar(Content content) => contentToBar.TryGetValue(content, out var bar) ? bar : null;
    public Dictionary<RectTransform, Content> GetBarMap() => barMap;
    public Dictionary<Content, RectTransform> GetContentToBar() => contentToBar;
    public HashSet<RectTransform> GetSelectedBars() => selectedBars;
    public RectTransform FindBarRT(PointerEventData ped) => FindBarRT(ped.pointerEnter);
}