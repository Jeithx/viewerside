using UnityEngine;
using UnityEngine.EventSystems;

public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRect;

    //private void OnEnable()
    //{
    //    rectTransform.anchoredPosition = new Vector2(0, 0);
    //}
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Parent Canvas'ı bul
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Drag başlangıcı için gerekli işlemler
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Paneli fare hareketine göre taşı
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        rectTransform.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvas == null || canvasRect == null) return;

        // Panelin sınırlarını kontrol et ve gerekirse yeniden konumlandır
        ClampToCanvas();
    }

    private void ClampToCanvas()
    {
        // Panelin sınırlarını hesapla (yerel uzayda)
        Vector3[] panelCorners = new Vector3[4];
        rectTransform.GetWorldCorners(panelCorners);

        // Dünya uzayındaki köşeleri Canvas'ın yerel uzayına çevir
        for (int i = 0; i < panelCorners.Length; i++)
        {
            panelCorners[i] = canvasRect.InverseTransformPoint(panelCorners[i]);
        }

        // Canvas sınırlarını al
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetLocalCorners(canvasCorners);

        // Panelin yeni pozisyonunu hesapla
        Vector2 newPosition = rectTransform.anchoredPosition;

        // Solda taşarsa
        if (panelCorners[0].x < canvasCorners[0].x)
        {
            newPosition.x += canvasCorners[0].x - panelCorners[0].x;
        }
        // Sağda taşarsa
        if (panelCorners[2].x > canvasCorners[2].x)
        {
            newPosition.x -= panelCorners[2].x - canvasCorners[2].x;
        }
        // Altta taşarsa
        if (panelCorners[0].y < canvasCorners[0].y)
        {
            newPosition.y += canvasCorners[0].y - panelCorners[0].y;
        }
        // Üstte taşarsa
        if (panelCorners[1].y > canvasCorners[1].y)
        {
            newPosition.y -= panelCorners[1].y - canvasCorners[1].y;
        }

        // Yeni pozisyonu uygula
        rectTransform.anchoredPosition = newPosition;
    }
}
