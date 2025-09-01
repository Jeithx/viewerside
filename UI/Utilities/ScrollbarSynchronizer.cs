using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// İki Scrollbar'ı birbirine senkronize eder. Biri hareket ettiğinde diğeri de onu takip eder.
/// </summary>
public class ScrollbarSynchronizer : MonoBehaviour
{
    [Header("Senkronize Edilecek Scrollbar'lar")]
    [Tooltip("İlk scrollbar (kaynak).")]
    public Scrollbar firstScrollbar;

    [Tooltip("İkinci scrollbar (hedef).")]
    public Scrollbar secondScrollbar;


    private bool isUpdating = false;

    void Start()
    {
        if (firstScrollbar == null || secondScrollbar == null)
        {
            Debug.LogError("Lütfen her iki Scrollbar referansını da Inspector üzerinden atayın!");
            return;
        }

        // Değer değişikliklerini dinlemek için listener'ları ekle
        firstScrollbar.onValueChanged.AddListener(OnFirstScrollbarChanged);
        secondScrollbar.onValueChanged.AddListener(OnSecondScrollbarChanged);
    }

    /// <summary>
    /// İlk scrollbar'ın değeri değiştiğinde tetiklenir.
    /// </summary>
    /// <param name="value">İlk scrollbar'ın yeni değeri (0 ile 1 arasında).</param>
    private void OnFirstScrollbarChanged(float value)
    {
        // Eğer güncelleme zaten başka bir yerden yapılıyorsa, tekrar yapma (sonsuz döngü önlemi)
        if (isUpdating) return;

        isUpdating = true;
        // İkinci scrollbar'ın değerini birincinin değeriyle aynı yap
        secondScrollbar.value = value;
        isUpdating = false;
    }

    /// <summary>
    /// İkinci scrollbar'ın değeri değiştiğinde tetiklenir.
    /// </summary>
    /// <param name="value">İkinci scrollbar'ın yeni değeri (0 ile 1 arasında).</param>
    private void OnSecondScrollbarChanged(float value)
    {
        // Eğer güncelleme zaten başka bir yerden yapılıyorsa, tekrar yapma (sonsuz döngü önlemi)
        if (isUpdating) return;

        isUpdating = true;
        // Birinci scrollbar'ın değerini ikincinin değeriyle aynı yap
        firstScrollbar.value = value;
        isUpdating = false;
    }

    void OnDestroy()
    {
        if (firstScrollbar != null)
        {
            firstScrollbar.onValueChanged.RemoveListener(OnFirstScrollbarChanged);
        }
        if (secondScrollbar != null)
        {
            secondScrollbar.onValueChanged.RemoveListener(OnSecondScrollbarChanged);
        }
    }
}