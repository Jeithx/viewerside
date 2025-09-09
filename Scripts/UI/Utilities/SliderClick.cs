using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderClick : MonoBehaviour, IPointerDownHandler
{
    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform sliderRect = slider.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            float width = sliderRect.rect.width;
            float localX = localPoint.x + width * 0.5f; 

            float pct = Mathf.Clamp01(localX / width);
            float newValue = Mathf.Lerp(slider.minValue, slider.maxValue, pct);

            ViewerCore.Instance.SetTime(newValue);

        }
    }
}