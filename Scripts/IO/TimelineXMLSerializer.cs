using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Video;

public class TimelineXMLSerializer : MonoBehaviour
{
    [Header("Core References")]
    public TimelinePlaybackManager playbackManager;
    public TimelineWindowManager windowManager;
    public Timer timer;

    //[Header("Pop-up Events")]
    //public bool scheduleEventFromXML = true;

    //private readonly List<Coroutine> _eventCoroutines = new();

    [Header("Content Prefabs")]
    public VideoPlayer videoPlayerPrefab;

    [Header("Model Registry")]
    public List<ModelEntry> modelRegistry = new List<ModelEntry>();

    private bool hasCleared = false;
    [Serializable]
    public class ModelEntry
    {
        public string name;
        public GameObject prefab;
    }

    public class PopupEventData
    {
        public int index;
        public string text;
        public float triggerTime;
    }

    public List<PopupEventData> LoadedPopupEvents { get; private set; } = new List<PopupEventData>();


    public void ImportFromXML(string filePath)
    {
        Debug.Log($"--- XML İÇE AKTARMA BAŞLATILDI: {filePath} ---");
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[TimelineXMLSerializer] HATA: Dosya bulunamadı: {filePath}");
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(filePath);
            var root = doc.Root;
            if (root.Name != "TimelineProject")
            {
                Debug.LogError("[TimelineXMLSerializer] HATA: Geçersiz XML formatı. Kök element 'TimelineProject' olmalı.");
                return;
            }
            Debug.Log("[TimelineXMLSerializer] XML dosyası başarıyla okundu. İçerikler temizleniyor...");
            ClearAll();
            StartCoroutine(ImportAfterClear(root));
        }
        catch (Exception e)
        {
            Debug.LogError($"[TimelineXMLSerializer] XML OKUMA HATASI: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnEnable()
    {
        //EventSource.OnSomethingHappened += ClearAll;
    }

    private void OnDisable()
    {
        //EventSource.OnSomethingHappened -= ClearAll;
    }

    public void ClearAll()
    {
        if (hasCleared) return;
        Debug.Log("[TimelineXMLSerializer] ClearAll: Mevcut tüm controller'lar ve pencereler siliniyor.");

        if (playbackManager != null)
        {
            playbackManager.Stop();

            foreach (var controller in playbackManager.videoControllers.Where(c => c != null))
            {
                var vc = controller.getvc();
                if (vc != null)
                {
                    var player = vc.GetVideoPlayer();
                    if (player != null)
                    {
                        Debug.Log($"VideoPlayer yok ediliyor: {player.name}");
                        if (Application.isPlaying)
                            Destroy(player.gameObject);
                        else
                            DestroyImmediate(player.gameObject);
                    }
                }

                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }

            foreach (var controller in playbackManager.modelControllers.Where(c => c != null))
            {
                var mc = controller.getmc();
                if (mc != null)
                {
                    var modelInstance = mc.GetModelInstance();
                    if (modelInstance != null)
                    {
                        Debug.Log($"Model Instance yok ediliyor: {modelInstance.name}");
                        if (Application.isPlaying)
                            Destroy(modelInstance);
                        else
                            DestroyImmediate(modelInstance);
                    }
                }

                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }

            foreach (var controller in playbackManager.imageControllers.Where(c => c != null))
            {
                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }

            foreach (var controller in playbackManager.audioControllers.Where(c => c != null))
            {
                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }

            playbackManager.videoControllers.Clear();
            playbackManager.imageControllers.Clear();
            playbackManager.audioControllers.Clear();
            playbackManager.modelControllers.Clear();
        }

        if (windowManager != null)
        {
            windowManager.ClearWindows();
        }

        Debug.Log("[TimelineXMLSerializer] ClearAll: Temizlik tamamlandı.");
    

    //foreach (var c in _eventCoroutines) if (c != null) StopCoroutine(c);
    //_eventCoroutines.Clear();

        LoadedPopupEvents.Clear();

        Debug.Log("[TimelineXMLSerializer] ClearAll: Temizlik tamamlandı.");

        hasCleared = true;

    }


    private IEnumerator ImportAfterClear(XElement root)
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("[TimelineXMLSerializer] Klipler ve kanallar işlenmeye başlıyor...");

        var tracks = root.Element("Tracks")?.Elements("Track");
        if (tracks != null)
        {
            var allClipData = tracks
               .SelectMany(track => track.Element("Clips")?.Elements("Clip") ?? Enumerable.Empty<XElement>(),
                           (track, clip) => new { clip, trackId = GetIntAttribute(track, "id", 0) })
               .OrderBy(item => GetFloatAttribute(item.clip, "start", 0f))
               .ToList();

            Debug.Log($"[TimelineXMLSerializer] Toplam {allClipData.Count} adet klip bulundu ve işlenmek üzere sıralandı.");

            foreach (var item in allClipData)
            {
                yield return StartCoroutine(ImportClip(item.clip, item.trackId));
            }
        }
        else
        {
            Debug.LogWarning("[TimelineXMLSerializer] XML içinde <Tracks> bölümü bulunamadı.");
        }

        //if (scheduleeventfromxml)
        //{
        //    var eventparent = root.element("popupevents")
        //                   ?? root.element("logs")
        //                   ?? root.element("notes");

        //    if (eventparent != null)
        //    {
        //        foreach (var ev in eventparent.elements()) // <event/> ya da <log/> vs. fark etmez
        //        {
        //            int index = getıntattribute(ev, "index", 0);
        //            string text = ev.attribute("text")?.value ?? ev.value ?? "";
        //            float trigger = getfloatattribute(ev, "trigger",
        //                             getfloatattribute(ev, "triggertime", 0f));

        //            var co = startcoroutine(scheduleeventlog(index, text, trigger));
        //            _eventcoroutines.add(co);
        //        }
        //    }
        //}

        #region Event Import (will be changed)

        LoadedPopupEvents.Clear();
        var eventParent = root.Element("PopupEvents") ?? root.Element("Logs") ?? root.Element("Notes");
        if (eventParent != null)
        {
            foreach (var ev in eventParent.Elements())
            {
                LoadedPopupEvents.Add(new PopupEventData
                {
                    index = GetIntAttribute(ev, "index", 0),
                    text = ev.Attribute("text")?.Value ?? ev.Value ?? "",
                    triggerTime = GetFloatAttribute(ev, "trigger",
                                   GetFloatAttribute(ev, "triggerTime", 0f))
                });
            }
        }

        #endregion


        hasCleared = false;
        Debug.Log("--- XML İÇE AKTARMA TAMAMLANDI ---");

        ViewerCore.Instance?.Play();
    }

    //private IEnumerator ScheduleEventLog(int index, string text, float triggerTime)
    //{
    //    if (timer)
    //    {
    //        yield return new WaitUntil(() =>
    //        {
    //            var t = timer.getCurrentTime();
    //            return t >= triggerTime;
    //        });
    //    }
    //    else
    //    {
    //        yield return new WaitForSeconds(Mathf.Max(0f, triggerTime));
    //    }

    //    Debug.Log($"[DebugEvent #{index}] {text} @t={triggerTime:F3}s");
    //}



    private IEnumerator ImportClip(XElement clipElement, int trackId)
    {
        string type = clipElement.Attribute("type")?.Value;
        float startTime = GetFloatAttribute(clipElement, "start", 0f);

        Debug.Log($"--- Klip İşleniyor: Tip = {type}, Başlangıç = {startTime:F2}s, Kanal ID = {trackId} ---");

        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning(">> UYARI: Klip türü belirtilmemiş, atlanıyor.");
            yield break;
        }

        Content newContent = null;

        switch (type)
        {
            case "Video":
                string videoPath = clipElement.Attribute("path")?.Value;
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    Debug.Log($">> Video oluşturuluyor: {videoPath}");
                    var vc = new VideoContent(videoPlayerPrefab, videoPath, startTime);
                    var host = new GameObject($"VideoController_{Path.GetFileNameWithoutExtension(videoPath)}");
                    var controller = VideoController.Create(host, timer, vc, windowManager);
                    playbackManager.videoControllers.Add(controller);
                    newContent = vc;
                    Debug.Log(">> BAŞARILI: Video controller oluşturuldu ve eklendi.");
                }
                else
                {
                    Debug.LogError($">> HATA: Video dosyası bulunamadı, atlanıyor: {videoPath}");
                }
                break;

            case "Image":
                string imagePath = clipElement.Attribute("path")?.Value;
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    Debug.Log($">> Resim oluşturuluyor: {imagePath}");
                    var ic = new ImageContent(imagePath, startTime);
                    ic.SetLength(GetFloatAttribute(clipElement, "duration", 20f));
                    var host = new GameObject($"ImageController_{Path.GetFileNameWithoutExtension(imagePath)}");
                    var controller = ImageController.Create(host, timer, ic, windowManager);
                    playbackManager.imageControllers.Add(controller);
                    newContent = ic;
                    Debug.Log(">> BAŞARILI: Resim controller oluşturuldu ve eklendi.");
                }
                else
                {
                    Debug.LogError($">> HATA: Resim dosyası bulunamadı, atlanıyor: {imagePath}");
                }
                break;

            case "Audio":
                string audioPath = clipElement.Attribute("path")?.Value;
                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    Debug.Log($">> Ses oluşturuluyor: {audioPath}");
                    var ac = new AudioContent(audioPath, startTime);
                    var host = new GameObject($"AudioController_{Path.GetFileNameWithoutExtension(audioPath)}");
                    var controller = AudioController.Create(host, timer, ac, audioPath);
                    playbackManager.audioControllers.Add(controller);
                    newContent = ac;
                    Debug.Log(">> BAŞARILI: Ses controller oluşturuldu ve eklendi.");
                }
                else
                {
                    Debug.LogError($">> HATA: Ses dosyası bulunamadı, atlanıyor: {audioPath}");
                }
                break;

            case "Model":
                string prefabName = clipElement.Attribute("prefabName")?.Value;
                Debug.Log($">> Model aranıyor: {prefabName}");
                GameObject modelPrefab = GetModelPrefab(prefabName);
                if (modelPrefab != null)
                {
                    Debug.Log(">> Model prefab'ı registry'de bulundu. Oluşturuluyor...");
                    string animName = clipElement.Attribute("animation")?.Value;
                    float duration = GetAnimationDuration(modelPrefab, animName);
                    var mc = new ModelContent(modelPrefab, startTime, duration);
                    if (!string.IsNullOrEmpty(animName)) mc.SetInitialAnimation(animName);

                    var host = new GameObject($"ModelController_{prefabName}");
                    var controller = ModelController.Create(host, timer, mc);
                    playbackManager.modelControllers.Add(controller);
                    newContent = mc;
                    Debug.Log(">> BAŞARILI: Model controller oluşturuldu ve eklendi.");
                }
                else
                {
                    Debug.LogError($">> HATA: Model prefab'ı '{prefabName}' Model Registry listesinde bulunamadı veya atanmamış, atlanıyor.");
                }
                break;
        }

        if (newContent != null)
        {
            newContent.SetLayer(trackId);
            var windowElement = clipElement.Element("Window");
            if (windowElement != null)
            {
                ApplyWindowData(newContent, windowElement);
            }
        }

        yield return new WaitForSeconds(0.05f);
    }

    private void ApplyWindowData(Content content, XElement windowElement)
    {
        Debug.Log(">> Pencere verisi bulundu, uygulanıyor...");
        windowManager.ShowContent(content, null);
        windowManager.HideContent(content);

        if (windowManager.GetWindows().TryGetValue(content, out var win) && win?.root != null)
        {
            RectTransform rootRT = win.root;
            float pX = GetFloatAttribute(windowElement, "posX", 0f);
            float pY = GetFloatAttribute(windowElement, "posY", 0f);
            float w = GetFloatAttribute(windowElement, "width", 800f);
            float h = GetFloatAttribute(windowElement, "height", 600f);
            Debug.Log($">> Pencere Ayarları: Pos({pX}, {pY}), Size({w}, {h})");

            rootRT.anchoredPosition = new Vector2(pX, pY);
            rootRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rootRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            rootRT.anchorMin = new Vector2(GetFloatAttribute(windowElement, "anchorMinX", 0.5f), GetFloatAttribute(windowElement, "anchorMinY", 0.5f));
            rootRT.anchorMax = new Vector2(GetFloatAttribute(windowElement, "anchorMaxX", 0.5f), GetFloatAttribute(windowElement, "anchorMaxY", 0.5f));
            rootRT.pivot = new Vector2(GetFloatAttribute(windowElement, "pivotX", 0.5f), GetFloatAttribute(windowElement, "pivotY", 0.5f));
            win.fitter?.FitNow();
        }
        else
        {
            Debug.LogError($">> HATA: İçerik için pencere oluşturulamadı: {content}");
        }
    }

    // ==================== HELPER METHODS ====================

    private GameObject GetModelPrefab(string name) => modelRegistry.FirstOrDefault(m => m.name == name)?.prefab;

    private float GetAnimationDuration(GameObject modelPrefab, string animationName)
    {
        if (modelPrefab == null || string.IsNullOrEmpty(animationName)) return 10f;
        Animator animator = modelPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName) return clip.length;
            }
        }
        return 10f;
    }

    private float GetFloatAttribute(XElement element, string name, float defaultValue)
    {
        string value = element.Attribute(name)?.Value;
        if (string.IsNullOrEmpty(value)) return defaultValue;
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