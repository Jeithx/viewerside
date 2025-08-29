using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WindowResizer : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    public RectTransform target;           // if null, uses this
    public RectTransform bounds;           // usually playbackStackRoot
    public float minWidth = 160f;
    public float minHeight = 90f;
    public float edge = 12f;               // grab margin in px
    public bool allowMove = true;

    private RectTransform _rt;
    private Canvas _canvas; // Canvas referansını saklamak için

    private Vector2 _dragStartLocal;
    private Vector2 _sizeAtDown;
    private Vector2 _posAtDown;
    private int _resizeMask;                // L=1, R=2, B=4, T=8

    void Start()
    {
        _rt = target ? target : GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>(); // Canvas referansını Awake'de alıyoruz

        // AWAKE DEBUG - Hiyerarşiyi de kontrol et
        Debug.Log($"[WindowResizer] Awake: _rt = {(_rt ? _rt.name : "null")}, _canvas = {(_canvas ? _canvas.name : "null")}");

        // Canvas hiyerarşisini debug et
        Transform current = transform;
        string hierarchy = "";
        while (current != null)
        {
            Canvas canvasOnThis = current.GetComponent<Canvas>();
            hierarchy += $"{current.name}({(canvasOnThis ? "HAS_CANVAS" : "no_canvas")}) -> ";
            current = current.parent;
        }
        Debug.Log($"[WindowResizer] Awake Hierarchy: {hierarchy}");

        var img = GetComponent<Image>();
        if (!img) { img = gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0.12f); }
        img.raycastTarget = true;

        Debug.Log($"[WindowResizer] Awake: Image raycastTarget = {img.raycastTarget}, GameObject = {gameObject.name}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[WindowResizer] OnPointerDown called on {gameObject.name}");

        if (_rt == null || _canvas == null)
        {
            Debug.LogError($"[WindowResizer] OnPointerDown: Missing components! _rt = {(_rt ? "OK" : "NULL")}, _canvas = {(_canvas ? "OK" : "NULL")}");
            return;
        }

        // Screen to local point conversion
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, eventData.position, _canvas.worldCamera, out _dragStartLocal);
        Debug.Log($"[WindowResizer] OnPointerDown: ScreenToLocal success = {success}, localPoint = {_dragStartLocal}, screenPos = {eventData.position}");

        _sizeAtDown = _rt.rect.size;
        _posAtDown = _rt.anchoredPosition;

        Debug.Log($"[WindowResizer] OnPointerDown: Initial size = {_sizeAtDown}, position = {_posAtDown}");

        // Resize mask calculation
        _resizeMask = 0;
        var r = _rt.rect;
        bool leftEdge = _dragStartLocal.x - r.xMin <= edge;
        bool rightEdge = r.xMax - _dragStartLocal.x <= edge;
        bool bottomEdge = _dragStartLocal.y - r.yMin <= edge;
        bool topEdge = r.yMax - _dragStartLocal.y <= edge;

        if (leftEdge) _resizeMask |= 1;
        if (rightEdge) _resizeMask |= 2;
        if (bottomEdge) _resizeMask |= 4;
        if (topEdge) _resizeMask |= 8;

        Debug.Log($"[WindowResizer] OnPointerDown: Rect = {r}, Edge distances: L={_dragStartLocal.x - r.xMin:F1}, R={r.xMax - _dragStartLocal.x:F1}, B={_dragStartLocal.y - r.yMin:F1}, T={r.yMax - _dragStartLocal.y:F1}");
        Debug.Log($"[WindowResizer] OnPointerDown: Resize mask = {_resizeMask} (L={leftEdge}, R={rightEdge}, B={bottomEdge}, T={topEdge})");
    }

    // IBeginDragHandler'ı eklemek, olayın daha güvenilir çalışmasını sağlar
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[WindowResizer] OnBeginDrag called on {gameObject.name}");
        // Bu metodun dolu olması gerekmiyor, arayüzü implemente etmesi yeterli.
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[WindowResizer] OnDrag called on {gameObject.name}");

        // Canvas'ı her seferinde kontrol et ve gerekirse tekrar bul
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            Debug.Log($"[WindowResizer] OnDrag: Re-searching canvas, found = {(_canvas ? _canvas.name : "NULL")}");
        }

        if (_rt == null || _canvas == null)
        {
            Debug.LogError($"[WindowResizer] OnDrag: Missing components! _rt = {(_rt ? "OK" : "NULL")}, _canvas = {(_canvas ? "OK" : "NULL")}");

            // Canvas hiyerarşisini debug et
            Transform current = transform;
            string hierarchy = "";
            while (current != null)
            {
                Canvas canvasOnThis = current.GetComponent<Canvas>();
                hierarchy += $"{current.name}({(canvasOnThis ? "HAS_CANVAS" : "no_canvas")}) -> ";
                current = current.parent;
            }
            Debug.LogError($"[WindowResizer] Hierarchy: {hierarchy}");

            return;
        }

        Vector2 curLocal;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, eventData.position, _canvas.worldCamera, out curLocal);
        Debug.Log($"[WindowResizer] OnDrag: ScreenToLocal success = {success}, currentLocal = {curLocal}, screenPos = {eventData.position}");

        Vector2 delta = curLocal - _dragStartLocal;
        Debug.Log($"[WindowResizer] OnDrag: Delta = {delta} (current: {curLocal} - start: {_dragStartLocal})");

        Vector2 size = _sizeAtDown;
        Vector2 pos = _posAtDown;

        bool resizing = _resizeMask != 0;
        Debug.Log($"[WindowResizer] OnDrag: Resizing = {resizing} (mask: {_resizeMask})");

        if (resizing)
        {
            Debug.Log("[WindowResizer] OnDrag: Performing RESIZE operation");
            Vector2 oldSize = size;
            Vector2 oldPos = pos;

            if ((_resizeMask & 1) != 0) { float w = Mathf.Max(minWidth, _sizeAtDown.x - delta.x); size.x = w; pos.x = _posAtDown.x + (delta.x * 0.5f); }
            if ((_resizeMask & 2) != 0) { float w = Mathf.Max(minWidth, _sizeAtDown.x + delta.x); size.x = w; pos.x = _posAtDown.x + (delta.x * 0.5f); }
            if ((_resizeMask & 4) != 0) { float h = Mathf.Max(minHeight, _sizeAtDown.y - delta.y); size.y = h; pos.y = _posAtDown.y + (delta.y * 0.5f); }
            if ((_resizeMask & 8) != 0) { float h = Mathf.Max(minHeight, _sizeAtDown.y + delta.y); size.y = h; pos.y = _posAtDown.y + (delta.y * 0.5f); }

            Debug.Log($"[WindowResizer] OnDrag: Size change: {oldSize} -> {size}, Pos change: {oldPos} -> {pos}");
        }
        else if (allowMove)
        {
            Debug.Log($"[WindowResizer] OnDrag: Performing MOVE operation (allowMove = {allowMove})");
            Vector2 oldPos = pos;
            // Pozisyonu delta'ya göre değil, canvas scale'ine göre direkt ayarlıyoruz
            pos = _rt.anchoredPosition + (eventData.delta / _canvas.scaleFactor);
            Debug.Log($"[WindowResizer] OnDrag: Move - eventData.delta = {eventData.delta}, scaleFactor = {_canvas.scaleFactor}");
            Debug.Log($"[WindowResizer] OnDrag: Position change: {oldPos} -> {pos}");
        }
        else
        {
            Debug.Log("[WindowResizer] OnDrag: No action taken (not resizing and allowMove is false)");
        }

        if (bounds)
        {
            Debug.Log($"[WindowResizer] OnDrag: Applying bounds constraint (bounds = {bounds.name})");
            var br = bounds.rect;
            float halfW = size.x * _rt.localScale.x * 0.5f; // scale'i hesaba kat
            float halfH = size.y * _rt.localScale.y * 0.5f;

            float minX = br.xMin + halfW;
            float maxX = br.xMax - halfW;
            float minY = br.yMin + halfH;
            float maxY = br.yMax - halfH;

            Vector2 oldPos = pos;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            Debug.Log($"[WindowResizer] OnDrag: Bounds rect = {br}, halfW = {halfW}, halfH = {halfH}");
            Debug.Log($"[WindowResizer] OnDrag: Clamp ranges: X[{minX:F1}, {maxX:F1}], Y[{minY:F1}, {maxY:F1}]");
            Debug.Log($"[WindowResizer] OnDrag: Position after clamp: {oldPos} -> {pos}");
        }

        // Apply changes
        Vector2 currentSize = _rt.rect.size;
        Vector2 currentPos = _rt.anchoredPosition;

        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        _rt.anchoredPosition = pos;

        Debug.Log($"[WindowResizer] OnDrag: APPLIED - Size: {currentSize} -> {size}, Position: {currentPos} -> {pos}");
        Debug.Log($"[WindowResizer] OnDrag: Final rect size = {_rt.rect.size}, anchoredPosition = {_rt.anchoredPosition}");

        var fitters = GetComponentsInChildren<RawImageFitter>(true);
        if (fitters.Length > 0)
        {
            Debug.Log($"[WindowResizer] OnDrag: Found {fitters.Length} RawImageFitters, calling FitNow()");
            for (int i = 0; i < fitters.Length; i++) fitters[i].FitNow();
        }

        Debug.Log($"[WindowResizer] OnDrag: ===== END DRAG FRAME =====");
    }
}