using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimelineTrackManager : MonoBehaviour
{
    private TimelineGrid timeline;
    private RectTransform tracksRoot;
    private readonly List<RectTransform> trackRows = new();
    private readonly List<bool> trackLockedStates = new List<bool>();
    private readonly List<TrackHeader> trackHeaders = new List<TrackHeader>();

    public void Initialize(TimelineGrid grid)
    {
        timeline = grid;
    }

    public void EnsureTracksRoot(RectTransform scrollViewContent, TimelineConfig config)
    {
        if (!tracksRoot)
        {
            var go = new GameObject("TracksRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            tracksRoot = go.GetComponent<RectTransform>();
            tracksRoot.SetParent(scrollViewContent, false);
            tracksRoot.anchorMin = new Vector2(0, 1);
            tracksRoot.anchorMax = new Vector2(0, 1);
            tracksRoot.pivot = new Vector2(0, 1);
            tracksRoot.anchoredPosition = Vector2.zero;
            tracksRoot.sizeDelta = Vector2.zero;

            var vlg = tracksRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = config.laneSpacing;
            vlg.padding = new RectOffset(0, 0, 38, 0);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tracksRoot);
        }
    }

    public RectTransform CreateRow(TimelineConfig config, GameObject headerPrefab, RectTransform headersParent)
    {
        var go = new GameObject($"Row{trackRows.Count}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(tracksRoot, false);

        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(timeline.GetContentWidth(), config.laneHeight);

        var img = go.GetComponent<Image>();
        img.color = config.laneColor;
        img.raycastTarget = true;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = config.laneHeight;

        AddCloseButtonToRow(rt);
        timeline.AttachRowTriggers(rt);

        trackRows.Add(rt);
        trackLockedStates.Add(false);

        if (headerPrefab != null && headersParent != null)
        {
            GameObject headerGO = Instantiate(headerPrefab, headersParent);
            TrackHeader header = headerGO.GetComponent<TrackHeader>();
            if (header != null)
            {
                //string trackType = timeline.GetCurrentTrackType();
                int trackIndex = trackRows.Count - 1;
                //char trackTypeFirst = trackType[0]; // abbreviate to first letter
                string defaultName = $"T{trackIndex}";
                header.Initialize(trackRows.Count - 1, timeline, defaultName);
                trackHeaders.Add(header);
            }
        }
        timeline.UpdateContentHeight();
        return rt;
    }

    public RectTransform InsertRowAt(int index, TimelineConfig config, GameObject headerPrefab, RectTransform headersParent)
    {
        var rt = CreateRow(config, headerPrefab, headersParent);
        int sibling = Mathf.Clamp(index, 0, tracksRoot.childCount - 1);
        rt.SetSiblingIndex(sibling);
        trackRows.Insert(Mathf.Clamp(index, 0, trackRows.Count), rt);
        timeline.UpdateContentHeight();
        //timeline.UpdateRowCloseButtons();
        return rt;
    }

    public void EnsureRowsAtLeast(int n, TimelineConfig config, GameObject headerPrefab, RectTransform headersParent)
    {
        while (trackRows.Count < n)
            CreateRow(config, headerPrefab, headersParent);
        //timeline.UpdateRowCloseButtons();
    }

    public int GetRowIndexOf(RectTransform row)
    {
        for (int i = 0; i < trackRows.Count; i++)
            if (trackRows[i] == row) return i;
        return -1;
    }

    public int GetAvailableTrackIndex()
    {
        for (int i = 0; i < trackRows.Count; i++)
        {
            if (!RowHasBars(trackRows[i]))
                return i;
        }

        EnsureRowsAtLeast(trackRows.Count + 1, timeline.GetConfig(), timeline.headerPrefab, timeline.headersParent);
        return trackRows.Count - 1;
    }

    private void AddCloseButtonToRow(RectTransform row)
    {
        var btnGO = new GameObject("CloseEmpty", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = btnGO.GetComponent<RectTransform>();
        rt.SetParent(row, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(18, 18);
        rt.anchoredPosition = new Vector2(-4, -4);

        var img = btnGO.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        var txtGO = new GameObject("X", typeof(RectTransform), typeof(Text));
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.SetParent(rt, false);
        txtRT.anchorMin = txtRT.anchorMax = new Vector2(0.5f, 0.5f);
        txtRT.pivot = new Vector2(0.5f, 0.5f);
        txtRT.sizeDelta = Vector2.zero;

        var t = txtGO.GetComponent<Text>();
        t.text = "×";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.fontSize = 14;
        t.raycastTarget = false;

        var b = btnGO.GetComponent<Button>();
        b.onClick.AddListener(() =>
        {
            if (RowHasBars(row)) return;
            RemoveRow(row);
        });

        btnGO.SetActive(false);
    }

    public bool RowHasBars(RectTransform row)
    {
        var contentToBar = timeline.GetBarManager().GetContentToBar();
        foreach (var kv in contentToBar)
            if (kv.Value && kv.Value.parent == row) return true;
        return false;
    }

    private void RemoveRow(RectTransform row)
    {
        if (row == null) return;
        if (RowHasBars(row)) return;
        int idx = GetRowIndexOf(row);
        if (idx < 0) return;

        trackRows.RemoveAt(idx);
        Destroy(row.gameObject);
        timeline.UpdateContentHeight();
        //timeline.UpdateRowCloseButtons();
    }

    //public void UpdateRowCloseButtons()
    //{
    //    foreach (var row in trackRows)
    //    {
    //        var close = row.Find("CloseEmpty");
    //        if (!close) continue;
    //        close.gameObject.SetActive(!RowHasBars(row));
    //    }
    //}

    public void SetTrackVisibility(int trackIndex, bool isVisible)
    {
        if (trackIndex < 0 || trackIndex >= trackRows.Count) return;

        RectTransform row = trackRows[trackIndex];
        foreach (Transform child in row)
        {
            if (child.name != "CloseEmpty")
            {
                child.gameObject.SetActive(isVisible);
            }

        }
    }

    public void SetTrackLock(int trackIndex, bool isLocked)
    {
        if (trackIndex < 0 || trackIndex >= trackLockedStates.Count) return;
        trackLockedStates[trackIndex] = isLocked;
    }

    public int CountEmptyRows()
    {
        int count = 0;
        foreach (var row in trackRows)
            if (!RowHasBars(row)) count++;
        return count;
    }

    public RectTransform GetRowUnderPointer(Vector2 screenPos, Canvas canvas)
    {
        if (trackRows.Count == 0) return null;
        var cam = canvas ? canvas.worldCamera : null;
        foreach (var row in trackRows)
            if (RectTransformUtility.RectangleContainsScreenPoint(row, screenPos, cam))
                return row;
        return null;
    }

    public void ClearTracks()
    {
        while (trackRows.Count > 1)
        {
            var row = trackRows[trackRows.Count - 1];
            if (row != null) Destroy(row.gameObject);
            trackRows.RemoveAt(trackRows.Count - 1);
        }

        trackLockedStates.Clear();
        trackLockedStates.Add(false);

        foreach (var header in trackHeaders)
        {
            if (header != null)
                Destroy(header.gameObject);
        }
        trackHeaders.Clear();
    }

    public void DeleteTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= trackRows.Count) return;

        var barManager = timeline.GetBarManager();
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

        timeline.UpdateContentHeight();
        Debug.Log($"Track {trackIndex} deleted from TrackManager");
    }

    public List<RectTransform> GetTrackRows() => trackRows;
    public List<bool> GetTrackLockedStates() => trackLockedStates;
    public List<TrackHeader> GetTrackHeaders() => trackHeaders;
    public RectTransform GetTracksRoot() => tracksRoot;
}