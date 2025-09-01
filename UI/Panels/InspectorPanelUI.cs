using UnityEngine;
using TMPro;

public class InspectorPanelUI : MonoBehaviour
{
    [Header("UI Metinleri")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI startTimeText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI endTimeText;
    [SerializeField] private TextMeshProUGUI animationNameText;

    private TimelineGrid timelineGrid;
    private Content currentContent;

    // Paneli başlatmak için kullanılır
    public void Initialize(TimelineGrid grid)
    {
        timelineGrid = grid;
        gameObject.SetActive(false); // Başlangıçta kapalı olsun
    }

    // Paneli göstermek ve bilgilerini doldurmak için kullanılır
    public void Show(Content content)
    {
        if (content == null) return;

        currentContent = content;
        gameObject.SetActive(true);
        UpdatePanelInfo();
    }

    // Paneli gizlemek için kullanılır
    public void Hide()
    {
        currentContent = null;
        gameObject.SetActive(false);
    }

    // Bilgileri günceller
    private void UpdatePanelInfo()
    {
        if (currentContent == null) return;

        // İsim bilgisini al
        string contentName = "Clip";
        if (currentContent is VideoContent vc)
            contentName = System.IO.Path.GetFileNameWithoutExtension(vc.getPath());
        else if (currentContent is ImageContent ic)
            contentName = System.IO.Path.GetFileNameWithoutExtension(ic.getPath());
        else if (currentContent is AudioContent ac)
            contentName = System.IO.Path.GetFileNameWithoutExtension(ac.getPath());
        else if (currentContent is ModelContent mc)
            contentName = "3D Model";

        // Zaman bilgilerini al
        float startTime = currentContent.getStart();
        float duration = timelineGrid.GetLengthOverride(currentContent, currentContent.getLength());
        float endTime = startTime + duration;

        // Metinleri güncelle
        nameText.text = $"Name: {contentName}";
        startTimeText.text = $"Start: {FormatTime(startTime)}";
        durationText.text = $"Duration: {FormatTime(duration)}";
        endTimeText.text = $"End: {FormatTime(endTime)}";

        if (currentContent is ModelContent modelContent)
        {
            animationNameText.gameObject.SetActive(true); // Metin alanını görünür yap
            string animName = modelContent.GetInitialAnimationName();
            animationNameText.text = string.IsNullOrEmpty(animName) ? "Animation: None" : $"Animation: {animName}";
        }
        else
        {
            animationNameText.gameObject.SetActive(false); // Model değilse metin alanını gizle
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
        int seconds = Mathf.FloorToInt(timeInSeconds - minutes * 60);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    public void InspectorPanelDeactivator()
    {
        this.gameObject.SetActive(false);
    }
}