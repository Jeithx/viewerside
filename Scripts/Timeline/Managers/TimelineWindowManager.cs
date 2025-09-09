using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimelineWindowManager : MonoBehaviour
{
    private RectTransform playbackStackRoot;

    public class Win
    {
        public RectTransform root;
        public RawImage view;
        public RawImageFitter fitter;
    }

    private readonly Dictionary<Content, Win> windows = new();

    public void Initialize(RectTransform stackRoot)
    {
        playbackStackRoot = stackRoot;
    }

    public void ShowContent(Content c, Texture tex)
    {
        var w = EnsureWindow(c, tex);
        if (w == null) return;
        if (!w.root.gameObject.activeSelf) w.root.gameObject.SetActive(true);
        if (tex) w.view.texture = tex;
        w.view.enabled = true;
        w.fitter?.FitNow(w.view.texture);
        w.root.SetAsLastSibling();
    }

    public void HideContent(Content c)
    {
        if (c != null && windows.TryGetValue(c, out var w) && w.root != null)
            w.root.gameObject.SetActive(false);
    }

    public bool WindowExistsFor(Content content)
    {
        return windows.ContainsKey(content);
    }

    public RectTransform GetWindowRoot(Content content)
    {
        if (windows.TryGetValue(content, out var win))
        {
            return win.root;
        }
        return null;
    }
    
    private Win EnsureWindow(Content c, Texture tex)
    {
        if (!playbackStackRoot) return null;
        if (windows.TryGetValue(c, out var have))
        {
            if (tex) have.view.texture = tex;
            return have;
        }

        var rootGO = new GameObject($"Win_{c.GetHashCode()}",
            typeof(RectTransform), typeof(Image));
        var rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.SetParent(playbackStackRoot, false);
        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = new Vector2(0.5f, 0.5f);

        var pSize = playbackStackRoot.rect.size;
        rootRT.sizeDelta = pSize;
        rootRT.anchoredPosition = Vector2.zero;

        var bg = rootGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.12f);
        bg.raycastTarget = true;

        var viewGO = new GameObject("View", typeof(RectTransform), typeof(RawImage), typeof(RawImageFitter));
        var viewRT = viewGO.GetComponent<RectTransform>();
        viewRT.SetParent(rootRT, false);
        viewRT.anchorMin = Vector2.zero; viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        var ri = viewGO.GetComponent<RawImage>();
        ri.texture = tex;
        ri.raycastTarget = false;
        ri.enabled = false;

        var fitter = viewGO.GetComponent<RawImageFitter>();
        fitter.referenceBounds = rootRT;
        fitter.mode = RawImageFitter.FitMode.Contain;
        fitter.fitBlackTexture = false;

        var win = new Win { root = rootRT, view = ri, fitter = fitter };
        windows[c] = win;

        rootGO.SetActive(false);
        return win;
    }

    public void OnRectTransformDimensionsChange()
    {
        foreach (var kv in windows)
        {
            var w = kv.Value;
            if (w.root != null && w.root.gameObject.activeInHierarchy)
                w.fitter?.FitNow(w.view ? w.view.texture : null);
        }
    }

    public void ClearWindows()
    {
        Debug.Log($"[TimelineWindowManager] ClearWindows başlatılıyor. Toplam {windows.Count} pencere var.");

        var windowsToDestroy = new List<GameObject>();

        foreach (var kvp in windows)
        {
            if (kvp.Value.root != null && kvp.Value.root.gameObject != null)
            {
                windowsToDestroy.Add(kvp.Value.root.gameObject);
                Debug.Log($"Yok edilecek pencere: {kvp.Value.root.gameObject.name}");
            }
        }

        foreach (var go in windowsToDestroy)
        {
            if (go != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
                Debug.Log($"Pencere yok edildi: {go.name}");
            }
        }

        windows.Clear();

        Debug.Log("[TimelineWindowManager] ClearWindows tamamlandı.");
    }

    public Dictionary<Content, Win> GetWindows() => windows;
}