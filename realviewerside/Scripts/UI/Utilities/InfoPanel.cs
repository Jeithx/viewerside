using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    public bool isPanelActive = false;

    public void TriggerPanel()
    {
        isPanelActive = !isPanelActive;
        Debug.Log("InfoPanel enabled");
        this.gameObject.SetActive(isPanelActive);
    }

}
