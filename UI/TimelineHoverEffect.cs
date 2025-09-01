using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimelineHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public float hoverAlpha = 0.8f;

    private Image imageComponent;
    private Color originalColor;
    private bool isHovering = false;

    [Header("Content Info")]
    public string contentType = "Unknown";
    public string contentName = "Unknown";

    public ModelContent connectedModelContent;
    private VideoContent connectedVideoContent;



    void Start()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            originalColor = imageComponent.color;
            normalColor = originalColor;
        }

        DetectContentInfo();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (imageComponent != null)
        {
            isHovering = true;
            Color hover = hoverColor;
            hover.a = hoverAlpha;
            imageComponent.color = hover;

            Debug.Log($"Hovering over: {contentType} - {contentName}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (imageComponent != null)
        {
            isHovering = false;
            imageComponent.color = originalColor;
        }
    }

    void DetectContentInfo()
    {
        if (transform.parent != null)
        {
            string parentName = transform.parent.name;

            if (parentName.Contains("Video"))
            {
                contentType = "Video";
                contentName = ExtractVideoName(parentName);
            }
            else if (parentName.Contains("Model"))
            {
                contentType = "Model";
                contentName = ExtractModelName(parentName);
            }
            else if (parentName.Contains("Animation"))
            {
                contentType = "Animation";
                contentName = ExtractAnimationName(parentName);
            }
            else
            {
                contentType = "Content";
                contentName = parentName;
            }
        }
    }

    string ExtractVideoName(string fullName)
    {
        // "VideoContent_0" -> "Video 0"
        return fullName.Replace("VideoContent_", "Video ");
    }

    string ExtractModelName(string fullName)
    {
        // Extract model name from parent or content
        return fullName.Replace("ModelContent_", "Model ");
    }

    string ExtractAnimationName(string fullName)
    {
        return fullName.Replace("AnimationContent_", "Animation ");
    }

    public void SetContentInfo(string type, string name)
    {
        contentType = type;
        contentName = name;
    }

    public bool IsHovering()
    {
        return isHovering;
    }

    public void SetHoverColor(Color color)
    {
        hoverColor = color;
    }

    void OnDestroy()
    {
        if (imageComponent != null && !isHovering)
        {
            imageComponent.color = originalColor;
        }
    }

    public void SetModelContent(ModelContent modelContent)
    {
        connectedModelContent = modelContent;
    }

    public ModelContent GetModelContent()
    {
        return connectedModelContent;
    }

    //public void SetVideoContent(VideoContent videoContent)
    //{
    //    connectedVideoContent = videoContent;
    //}

    //public VideoContent GetVideoContent()
    //{
    //    return connectedVideoContent;
    //}
}