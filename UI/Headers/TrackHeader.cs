using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackHeader : MonoBehaviour
{
    [Header("UI References")]
    public Button deleteButton;
    public Button visibilityButton;
    public Image visibilityIcon;
    public Button lockButton;
    public Image lockIcon;

    [Header("Icon Sprites")]

    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;
    [SerializeField] private Sprite lockOnSprite;      
    [SerializeField] private Sprite lockOffSprite;

    [SerializeField] private TextMeshProUGUI trackLabelText;


    private int trackIndex;
    private TimelineGrid timeline;
    private string trackType;
    private bool isVisible = true;
    private bool isLocked = false;

    public void Initialize(int index, TimelineGrid grid, string type)
    {
        trackIndex = index;
        timeline = grid;
        trackType = type;



        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        if (visibilityButton != null)
        {
            visibilityButton.onClick.RemoveAllListeners();
            visibilityButton.onClick.AddListener(OnVisibilityButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.RemoveAllListeners();
            lockButton.onClick.AddListener(OnLockButtonClicked);
        }

        UpdateVisibilityIcon();
        UpdateLockIcon();
        UpdateTrackText();
    }

    private void OnLockButtonClicked()

    {
        isLocked = !isLocked;
        if (HasContent())
        {
            timeline.SetTrackLock(trackIndex, isLocked);
            
        }
        UpdateLockIcon();

    }

    private void UpdateTrackText()
    {
        if (trackLabelText != null)
        {
            trackLabelText.text = trackType;
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (HasContent())
        {
            Debug.Log($"Cannot delete track {trackIndex}: contains content");
            return;
        }

        Debug.Log($"Deleting empty track {trackIndex}");
        timeline.DeleteTrack(trackIndex);
    }

    private void OnVisibilityButtonClicked()
    {
        isVisible = !isVisible;
        timeline.SetTrackVisibility(trackIndex, isVisible);
        UpdateVisibilityIcon();

        Debug.Log($"Track {trackIndex} visibility: {isVisible}");
    }

    private void UpdateVisibilityIcon()
    {
        if (visibilityIcon != null)
        {
            if (eyeOpenSprite != null && eyeClosedSprite != null)
            {
                visibilityIcon.sprite = isVisible ? eyeOpenSprite : eyeClosedSprite;
            }
            visibilityIcon.color = isVisible ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
    }

    private void UpdateLockIcon()
    {
        if (lockOnSprite != null && lockOffSprite != null)
        {
            lockIcon.sprite = isLocked ? lockOffSprite : lockOnSprite;
        }
    }


    private bool HasContent()
    {
        var trackRows = timeline.GetTrackRows();
        if (trackIndex >= trackRows.Count) return false;

        var barManager = timeline.GetBarManager();
        foreach (var kvp in barManager.GetContentToBar())
        {
            if (kvp.Value != null && kvp.Value.parent == trackRows[trackIndex])
                return true;
        }
        return false;
    }

    public void UpdateTrackIndex(int newIndex)
    {
        trackIndex = newIndex;
    }

    public bool GetVisibility() => isVisible;
    public void SetVisibility(bool visible)
    {
        isVisible = visible;
        UpdateVisibilityIcon();
    }
}