using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class TimelineContextMenu : MonoBehaviour, IPointerClickHandler
{
    [Header("Context Menu")]
    public GameObject contextMenuPrefab;
    public TimelineGrid timelineGrid;

    private GameObject activeContextMenu;

    public void OnPointerClick(PointerEventData eventData)
    {

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("Right-click context menu is disabled. Animations are now selected when adding models.");
        }
    }

    public void SetContextMenuPrefab(GameObject prefab)
    {
        contextMenuPrefab = prefab;
    }

    void CloseContextMenu()
    {
        if (activeContextMenu != null)
        {
            Destroy(activeContextMenu);
            activeContextMenu = null;
        }
    }
}