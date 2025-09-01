using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MediaItemUI : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button itemButton;

    private string assetPath;
    private System.Type assetType;
    private MediaBrowser browser;

    // Initialize fonksiyonu artık bir de Sprite parametresi alıyor
    public void Initialize(string path, System.Type type, MediaBrowser mediaBrowser, Sprite icon)
    {
        assetPath = path;
        assetType = type;
        browser = mediaBrowser;

        nameText.text = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        itemButton.onClick.AddListener(OnItemClicked);

        // Gelen ikonu doğrudan ata
        if (icon != null)
        {
            thumbnailImage.sprite = icon;
        }
    }

    private void OnItemClicked()
    {
        browser.OnMediaItemSelected(assetPath, assetType);
    }
}