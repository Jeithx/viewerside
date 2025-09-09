using UnityEngine;
using UnityEngine.UI;

public class InfoPanelController : MonoBehaviour
{
    private bool isPanelActive = true;

    public void TriggerPanel()
    {
        isPanelActive = !isPanelActive;

        var layout = GetComponent<LayoutElement>();
        if (isPanelActive)
        {
            layout.preferredHeight = 80f;
        }
        else
        {
            layout.preferredHeight = 0f;
        }

        Debug.Log("InfoPanel state: " + (isPanelActive ? "Open" : "Closed"));
    }
}

