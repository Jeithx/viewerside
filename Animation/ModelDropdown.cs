using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModelDropdown : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown modelDropdown;
    public Button addButton;
    public TimelineGrid timelineGrid;

    [Header("Animation Selection")]
    public GameObject animationSelectionWindowPrefab;
    public Transform canvasTransform;

    [Header("Available Models")]
    public GameObject[] availableModels;

    private int selectedModelIndex = 0;
    private AnimationSelectionWindow animSelectionWindow;

    void Start()
    {
        PopulateDropdown();
        SetupButtons();

        // Create animation selection window component
        GameObject windowManager = new GameObject("AnimationSelectionWindow");
        animSelectionWindow = windowManager.AddComponent<AnimationSelectionWindow>();
        animSelectionWindow.windowPrefab = animationSelectionWindowPrefab;
        animSelectionWindow.canvasTransform = canvasTransform;
    }

    void PopulateDropdown()
    {
        if (modelDropdown == null) return;

        modelDropdown.ClearOptions();
        modelDropdown.options.Add(new TMP_Dropdown.OptionData("Select Model..."));

        foreach (GameObject model in availableModels)
        {
            if (model != null)
            {
                modelDropdown.options.Add(new TMP_Dropdown.OptionData(model.name));
            }
        }

        modelDropdown.value = 0;
        modelDropdown.RefreshShownValue();
    }

    void SetupButtons()
    {
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddButtonClicked);
        }

        if (modelDropdown != null)
        {
            modelDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    void OnDropdownValueChanged(int index)
    {
        selectedModelIndex = index;

        if (addButton != null)
        {
            addButton.interactable = (index > 0 && index <= availableModels.Length);
        }
    }

    void OnAddButtonClicked()
    {
        if (selectedModelIndex <= 0 || selectedModelIndex > availableModels.Length)
        {
            Debug.LogWarning("No valid model selected!");
            return;
        }

        if (timelineGrid == null)
        {
            Debug.LogError("TimelineGrid reference is missing!");
            return;
        }

        GameObject selectedModel = availableModels[selectedModelIndex - 1];

        if (selectedModel == null)
        {
            Debug.LogError("Selected model is null!");
            return;
        }

        ShowAnimationSelectionForModel(selectedModel);
    }

    void ShowAnimationSelectionForModel(GameObject model)
    {
        if (animSelectionWindow == null)
        {
            Debug.LogError("Animation selection window not initialized!");
            return;
        }

        // callback that getting called automatically
        animSelectionWindow.ShowAnimationSelection(model, (animationName, duration) => {
            AddModelWithAnimation(model, animationName, duration);
        });
    }

    void AddModelWithAnimation(GameObject modelPrefab, string animationName, float duration)
    {
        timelineGrid.addModelToTimeline(modelPrefab, duration);

        if (timelineGrid.modelControllerList.Count > 0)
        {
            var lastController = timelineGrid.modelControllerList[timelineGrid.modelControllerList.Count - 1];

            // add the animation to the time 0 as my mentor said so
            lastController.getmc().SetInitialAnimation(animationName);

            Debug.Log($"Added {modelPrefab.name} with animation {animationName} for {duration} seconds");
        }
    }
}