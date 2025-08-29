using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class RawImageFitter : MonoBehaviour
{
    public enum FitMode { Contain, Cover, Stretch }

    [Header("Sizing")]
    public FitMode mode = FitMode.Contain;
    [Tooltip("Bounds to fit inside. If null, uses this RawImage's parent RectTransform.")]
    public RectTransform referenceBounds;

    [Tooltip("Resize when showing the 'black' texture too.")]
    public bool fitBlackTexture = false;

    private RawImage _ri;
    private RectTransform _rt;

    void Awake()
    {
        _ri = GetComponent<RawImage>();
        _rt = GetComponent<RectTransform>();
    }

    void OnEnable() { FitNow(); }
    void Update()
    {
#if UNITY_EDITOR
        // keep it looking right in-editor too
        if (!Application.isPlaying) FitNow();
#endif
    }

    public void FitNow(Texture texOverride = null)
    {
        if (_ri == null || _rt == null) return;
        var bounds = referenceBounds ? referenceBounds : _rt.parent as RectTransform;
        if (!bounds) return;

        Texture tex = texOverride ? texOverride : _ri.texture;
        if (!tex) return;

        // If it's the tiny black filler and user doesn't want to resize on black, bail.
        if (!fitBlackTexture && tex.width <= 2 && tex.height <= 2) return;

        float bw = Mathf.Max(1f, bounds.rect.width);
        float bh = Mathf.Max(1f, bounds.rect.height);
        float ba = bw / bh;

        float ta = Mathf.Max(1f, (float)tex.width) / Mathf.Max(1f, (float)tex.height);

        float w = bw, h = bh;

        switch (mode)
        {
            case FitMode.Contain: // letterbox
                if (ta > ba) { w = bw; h = w / ta; }
                else { h = bh; w = h * ta; }
                break;

            case FitMode.Cover: // crop-fill
                if (ta > ba) { h = bh; w = h * ta; }
                else { w = bw; h = w / ta; }
                break;

            case FitMode.Stretch:
                w = bw; h = bh;
                break;
        }

        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        _rt.anchoredPosition = Vector2.zero; // center inside parent
    }
}
