using UnityEngine;

[CreateAssetMenu(fileName = "TimelineConfig", menuName = "Timeline/Config")]
public class TimelineConfig : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridCellHorizontalPixelCount = 20;

    [Header("Track Settings")]
    public float laneHeight = 60f;
    public float laneSpacing = 6f;
    public Color laneColor = new Color(0.16f, 0.16f, 0.16f, 0.95f);

    [Header("Scroll Content")]
    public float contentRightMarginPx = 200f;
    public float verticalMargin = 20f;

    [Header("Zoom Settings")]
    public float minZoom = 5f;
    public float maxZoom = 200f;
    public float zoomSpeed = 1.2f;

    [Header("Snap Settings")]
    public float snapPixelThreshold = 10f;
    public float minImageDurationSeconds = 0.5f;

    [Header("Time Ruler")]
    public float rulerHeight = 30f;
    public Color majorTickColor = Color.white;
    public Color minorTickColor = Color.gray;
    public Color timeTextColor = Color.white;

    [Header("Bar Previews")]
    public int previewCount = 4;
    public float previewPadding = 2f;
    public bool enablePreviews = true;
}