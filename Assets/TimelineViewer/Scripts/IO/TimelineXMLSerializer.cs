using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Globalization;

public class TimelineXMLSerializer : MonoBehaviour
{
    [Header("Core References")]
    public TimelinePlaybackManager playbackManager;
    public TimelineWindowManager windowManager;
    public TimelineConfig config;
    public Timer timer;
    public List<RectTransform> trackRows; // Timeline kanallarının (row) parent'ları

    [Header("Content Prefabs")]
    public VideoPlayer videoPlayerPrefab; // VideoPlayer prefab'ı
    public GameObject videoBarPrefab;
    public GameObject imageBarPrefab;
    public GameObject audioBarPrefab;
    public GameObject modelBarPrefab;

    [Header("Model Registry")]
    public List<ModelEntry> modelRegistry = new List<ModelEntry>();

    [Serializable]
    public class ModelEntry
    {
        public string name;
        public GameObject prefab;
    }

    // ==================== IMPORT ====================

    public void ImportFromXML(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[TimelineXMLSerializer] Dosya bulunamadı: {filePath}");
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(filePath);
            var root = doc.Root;
            if (root.Name != "TimelineProject")
            {
                Debug.LogError("[TimelineXMLSerializer] Geçersiz XML formatı: Kök element 'TimelineProject' olmalı.");
                return;
            }

            ClearAll();
            StartCoroutine(ImportAfterClear(root));
        }
        catch (Exception e)
        {
            Debug.LogError($"[TimelineXMLSerializer] XML içe aktarma başarısız: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ClearAll()
    {
        // Önceki tüm denetleyicileri ve içerikleri temizle
        if (playbackManager != null)
        {
            // Kontrolcüleri yok et
            foreach (var controller in playbackManager.videoControllers.Where(c => c != null)) Destroy(controller.gameObject);
            foreach (var controller in playbackManager.imageControllers.Where(c => c != null)) Destroy(controller.gameObject);
            foreach (var controller in playbackManager.audioControllers.Where(c => c != null)) Destroy(controller.gameObject);
            foreach (var controller in playbackManager.modelControllers.Where(c => c != null)) Destroy(controller.gameObject);

            // Listeleri temizle
            playbackManager.videoControllers.Clear();
            playbackManager.imageControllers.Clear();
            playbackManager.audioControllers.Clear();
            playbackManager.modelControllers.Clear();
        }

        // Pencereleri temizle
        if (windowManager != null)
        {
            windowManager.ClearWindows();
        }
    }

    private IEnumerator ImportAfterClear(XElement root)
    {
        // ClearAll'dan sonra bir frame bekleyerek objelerin yok edilmesini garantile
        yield return new WaitForEndOfFrame();

        // Ayarları yükle
        var settingsElement = root.Element("Settings");
        if (settingsElement != null && config != null)
        {
            config.gridCellHorizontalPixelCount = GetIntAttribute(settingsElement, "gridCellPixels", 20);
            config.laneHeight = GetFloatAttribute(settingsElement, "laneHeight", 60f);
            config.snapPixelThreshold = GetFloatAttribute(settingsElement, "snapThreshold", 10f);
            config.enablePreviews = GetBoolAttribute(settingsElement, "enablePreviews", true);
        }

        var tracks = root.Element("Tracks")?.Elements("Track");
        if (tracks != null)
        {
            // Klipleri başlangıç zamanına göre sıralayarak içe aktar
            var allClipData = tracks
               .SelectMany(track => track.Element("Clips")?.Elements("Clip") ?? new XElement[0],
                           (track, clip) => new { clip, trackId = GetIntAttribute(track, "id", 0) })
               .OrderBy(item => GetFloatAttribute(item.clip, "start", 0f))
               .ToList();

            foreach (var item in allClipData)
            {
                yield return StartCoroutine(ImportClip(item.clip, item.trackId));
            }
        }

        Debug.Log("[TimelineXMLSerializer] Timeline içe aktarma tamamlandı.");
    }

    private IEnumerator ImportClip(XElement clipElement, int trackId)
    {
        string type = clipElement.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("[TimelineXMLSerializer] Klip türü belirtilmemiş, atlanıyor.");
            yield break;
        }

        if (trackId < 0 || trackId >= trackRows.Count)
        {
            Debug.LogError($"[TimelineXMLSerializer] Geçersiz trackId: {trackId}. 'trackRows' listesinde bu indekse sahip bir eleman yok.");
            yield break;
        }

        RectTransform parentRow = trackRows[trackId];
        float startTime = GetFloatAttribute(clipElement, "start", 0f);
        Content newContent = null;

        switch (type)
        {
            case "Video":
                string videoPath = clipElement.Attribute("path")?.Value;
                if (File.Exists(videoPath))
                {
                    var vc = new VideoContent(videoPlayerPrefab, videoPath, startTime);
                    var host = new GameObject($"VideoController_{Path.GetFileNameWithoutExtension(videoPath)}");
                    var controller = VideoController.Create(host, timer, vc, windowManager);
                    playbackManager.videoControllers.Add(controller);
                    newContent = vc;
                }
                break;

            case "Image":
                string imagePath = clipElement.Attribute("path")?.Value;
                if (File.Exists(imagePath))
                {
                    var ic = new ImageContent(imagePath, startTime);
                    ic.SetLength(GetFloatAttribute(clipElement, "duration", 20f)); // Süreyi XML'den ayarla
                    var host = new GameObject($"ImageController_{Path.GetFileNameWithoutExtension(imagePath)}");
                    var controller = ImageController.Create(host, timer, ic, windowManager);
                    playbackManager.imageControllers.Add(controller);
                    newContent = ic;
                }
                break;

            case "Audio":
                string audioPath = clipElement.Attribute("path")?.Value;
                if (File.Exists(audioPath))
                {
                    var ac = new AudioContent(audioBarPrefab, audioPath, config.gridCellHorizontalPixelCount, parentRow, startTime);
                    var host = new GameObject($"AudioController_{Path.GetFileNameWithoutExtension(audioPath)}");
                    var controller = AudioController.Create(host, timer, ac, audioPath);
                    playbackManager.audioControllers.Add(controller);
                    newContent = ac;
                }
                break;

            case "Model":
                string prefabName = clipElement.Attribute("prefabName")?.Value;
                GameObject modelPrefab = GetModelPrefab(prefabName);
                if (modelPrefab != null)
                {
                    string animName = clipElement.Attribute("animation")?.Value;
                    float duration = GetAnimationDuration(modelPrefab, animName); // Süreyi animasyondan al
                    var mc = new ModelContent(modelPrefab,startTime, duration);
                    if (!string.IsNullOrEmpty(animName)) mc.SetInitialAnimation(animName);

                    var host = new GameObject($"ModelController_{prefabName}");
                    var controller = ModelController.Create(host, timer, mc);
                    playbackManager.modelControllers.Add(controller);
                    newContent = mc;
                }
                break;
        }

        if (newContent != null)
        {
            newContent.SetLayer(trackId);
            newContent.SetStart(startTime);

            var windowElement = clipElement.Element("Window");
            if (windowElement != null)
            {
                // Pencereyi oluştur ve ayarlarını uygula
                ApplyWindowData(newContent, windowElement);
            }
        }

        // İçeriklerin yüklenmesi için kısa bir bekleme süresi
        yield return new WaitForSeconds(0.05f);
    }

    private void ApplyWindowData(Content content, XElement windowElement)
    {
        // Önce pencerenin var olduğundan emin ol (ShowContent boş bir texture ile çağrılabilir)
        windowManager.ShowContent(content, null);
        windowManager.HideContent(content); // Başlangıçta gizli kalsın

        var win = windowManager.GetWindows().ContainsKey(content) ? windowManager.GetWindows()[content] : null;
        if (win?.root == null)
        {
            Debug.LogError($"[TimelineXMLSerializer] İçerik için pencere oluşturulamadı: {content}");
            return;
        }

        RectTransform rootRT = win.root;

        rootRT.anchoredPosition = new Vector2(
            GetFloatAttribute(windowElement, "posX", 0f),
            GetFloatAttribute(windowElement, "posY", 0f)
        );
        rootRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetFloatAttribute(windowElement, "width", 800f));
        rootRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetFloatAttribute(windowElement, "height", 600f));

        rootRT.anchorMin = new Vector2(
            GetFloatAttribute(windowElement, "anchorMinX", 0.5f),
            GetFloatAttribute(windowElement, "anchorMinY", 0.5f)
        );
        rootRT.anchorMax = new Vector2(
            GetFloatAttribute(windowElement, "anchorMaxX", 0.5f),
            GetFloatAttribute(windowElement, "anchorMaxY", 0.5f)
        );
        rootRT.pivot = new Vector2(
            GetFloatAttribute(windowElement, "pivotX", 0.5f),
            GetFloatAttribute(windowElement, "pivotY", 0.5f)
        );

        win.fitter?.FitNow();
    }


    // ==================== HELPER METHODS ====================

    private GameObject GetModelPrefab(string name) => modelRegistry.FirstOrDefault(m => m.name == name)?.prefab;

    private float GetAnimationDuration(GameObject modelPrefab, string animationName)
    {
        if (modelPrefab == null || string.IsNullOrEmpty(animationName)) return 10f; // Varsayılan süre

        Animator animator = modelPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName) return clip.length;
            }
        }
        return 10f; // Animasyon bulunamazsa varsayılan süre
    }

    private float GetFloatAttribute(XElement element, string name, float defaultValue)
    {
        string value = element.Attribute(name)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        value = value.Replace(',', '.');

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }

        return defaultValue;
    }

    private int GetIntAttribute(XElement element, string name, int defaultValue)
    {
        string value = element.Attribute(name)?.Value;
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private bool GetBoolAttribute(XElement element, string name, bool defaultValue)
    {
        string value = element.Attribute(name)?.Value;
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }
}