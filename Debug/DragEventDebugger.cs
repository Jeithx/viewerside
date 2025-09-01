using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Bu script, sahnede tek bir yerde durarak tüm UI sürükleme olaylarını
// manuel olarak tespit eder ve konsola loglar.
public class DragEventDebugger : MonoBehaviour
{
    [Tooltip("Bir sürüklemenin başlaması için farenin kaç piksel hareket etmesi gerektiği.")]
    public float dragThreshold = 5f;

    private bool isDragging = false;
    private GameObject dragCandidate = null;
    private Vector2 startMousePosition;

    void Update()
    {
        // Fare sol tuşuna basıldığında
        if (Input.GetMouseButtonDown(0))
        {
            // O an farenin altındaki nesneyi sürükleme adayı olarak belirle
            dragCandidate = GetObjectUnderMouse();
            startMousePosition = Input.mousePosition;
        }

        // Fare sol tuşu basılı tutuluyorsa
        if (Input.GetMouseButton(0))
        {
            // Eğer bir adayımız varsa ve henüz sürükleme başlamadıysa
            if (dragCandidate != null && !isDragging)
            {
                // Fare, başlangıç pozisyonundan yeterince uzaklaştı mı?
                if (Vector2.Distance(startMousePosition, Input.mousePosition) > dragThreshold)
                {
                    // Evet, uzaklaştı. Sürüklemeyi başlat ve konsola log at.
                    isDragging = true;
                    Debug.LogWarning($"[Drag Detector] Sürükleme başladı: {dragCandidate.name}");
                }
            }
        }

        // Fare sol tuşu bırakıldığında
        if (Input.GetMouseButtonUp(0))
        {
            // Tüm durumları sıfırla
            if (isDragging)
            {
                Debug.Log("[Drag Detector] Sürükleme bitti.");
            }
            isDragging = false;
            dragCandidate = null;
        }
    }

    /// <summary>
    /// Fare imlecinin altındaki en üstteki UI nesnesini döndürür.
    /// </summary>
    private GameObject GetObjectUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Eğer en az bir sonuç varsa, en üsttekini (listedeki ilk elemanı) döndür.
        if (results.Count > 0)
        {
            return results[0].gameObject;
        }

        return null;
    }
}