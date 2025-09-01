using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class MediaBrowser : MonoBehaviour
{
    [Header("Ana Referanslar")]
    public TimelineGrid timelineGrid;
    public GameObject browserPanel;

    [Header("Panel Elemanları")]
    public GameObject categoryButtonsContainer; 
    public GameObject contentScrollView;        
    public GameObject backButton;               

    [Header("Liste Elemanları")]
    public RectTransform contentParent;
    public GameObject mediaItemPrefab;

    [Header("Klasör Yolları")]
    private string videosPath = "Assets/ProjectMedia/Videos";
    private string imagesPath = "Assets/ProjectMedia/Images";
    private string audiosPath = "Assets/ProjectMedia/Audios";

    [Header("İkonlar")]
    public Sprite videoIcon;
    public Sprite imageIcon;
    public Sprite audioIcon;

    // Bu fonksiyon ana '+' butonuna bağlı kalacak
    public void ToggleBrowser()
    {
        browserPanel.SetActive(!browserPanel.activeSelf);
        // Tarayıcı her açıldığında, ana kategori ekranını göstererek sıfırlanır.
        if (browserPanel.activeSelf)
        {
            ShowCategoryView();
        }
    }

    // --- KATEGORİ SEÇİM FONKSİYONLARI ---
    public void ShowVideos()
    {
        ShowContentView(); // İçerik görünümüne geç
        PopulateBrowser(videosPath, typeof(UnityEngine.Video.VideoClip));
    }

    public void ShowImages()
    {
        ShowContentView();
        PopulateBrowser(imagesPath, typeof(Texture2D));
    }

    public void ShowAudios()
    {
        ShowContentView();
        PopulateBrowser(audiosPath, typeof(AudioClip));
    }

    // --- GÖRÜNÜM DEĞİŞTİRME FONKSİYONLARI ---

    // Bu fonksiyonu "Geri" butonuna bağlayacağız
    public void ShowCategoryView()
    {
        categoryButtonsContainer.SetActive(true); // Kategori butonlarını göster
        contentScrollView.SetActive(false);       // İçerik listesini gizle
        backButton.SetActive(false);              // Geri butonunu gizle
    }

    // Bu, bir kategori seçildiğinde otomatik çalışır
    private void ShowContentView()
    {
        categoryButtonsContainer.SetActive(false); // Kategori butonlarını gizle
        contentScrollView.SetActive(true);        // İçerik listesini göster
        backButton.SetActive(true);               // Geri butonunu göster
    }

    private void PopulateBrowser(string folderPath, System.Type assetType)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        string[] guids = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { folderPath });

        // Hangi ikonun kullanılacağını başta belirle
        Sprite iconToUse = null;
        if (assetType == typeof(UnityEngine.Video.VideoClip)) iconToUse = videoIcon;
        else if (assetType == typeof(Texture2D)) iconToUse = imageIcon;
        else if (assetType == typeof(AudioClip)) iconToUse = audioIcon;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject itemGO = Instantiate(mediaItemPrefab, contentParent);

            // Yeni MediaItemUI script'ini al ve bu sefer ikonla birlikte başlat
            MediaItemUI itemUI = itemGO.GetComponent<MediaItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(assetPath, assetType, this, iconToUse);
            }
        }
    }

    public void OnMediaItemSelected(string path, System.Type assetType)
    {
        if (assetType == typeof(UnityEngine.Video.VideoClip))
        {
            timelineGrid.addVideoClip(path);
        }
        else if (assetType == typeof(Texture2D))
        {
            timelineGrid.addImage(path);
        }
        else if (assetType == typeof(AudioClip))
        {
            timelineGrid.addAudio(path);
        }

        browserPanel.SetActive(false);
    }
}