using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
[RequireComponent(typeof(RectTransform))]
public class DynamicLayoutSpacing : MonoBehaviour
{
    [SerializeField] private float spacingValueA = 35f;
    [SerializeField] private float spacingValueB = 67f;

    private HorizontalLayoutGroup layoutGroup;
    private RectTransform rectTransform;

    private bool isChangingInternally = true;

    void Awake()
    {
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnRectTransformDimensionsChange() { 

        if (isChangingInternally)
        {
            isChangingInternally = false;
            return;
        }

        ToggleSpacing();
    }

    private void ToggleSpacing()
    {
        float currentSpacing = layoutGroup.spacing;
        float distanceToA = Mathf.Abs(currentSpacing - spacingValueA);
        float distanceToB = Mathf.Abs(currentSpacing - spacingValueB);

        float newSpacing = (distanceToB > distanceToA) ? spacingValueB : spacingValueA;

        if (!Mathf.Approximately(currentSpacing, newSpacing))
        {
            isChangingInternally = true;
            layoutGroup.spacing = newSpacing;
        }
    }
}