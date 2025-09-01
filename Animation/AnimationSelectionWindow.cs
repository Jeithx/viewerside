using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AnimationSelectionWindow : MonoBehaviour
{
    [Header("UI References")]
    public GameObject windowPrefab;
    public Transform canvasTransform;

    private GameObject activeWindow;
    private GameObject modelPrefab;
    private System.Action<string, float> onAnimationSelected;

    public void ShowAnimationSelection(GameObject model, System.Action<string, float> callback)
    {
        modelPrefab = model;
        onAnimationSelected = callback;

        if (activeWindow != null)
            Destroy(activeWindow);

        activeWindow = Instantiate(windowPrefab, canvasTransform);
        activeWindow.transform.localPosition = Vector3.zero;

        Canvas windowCanvas = activeWindow.GetComponent<Canvas>();
        //if (windowCanvas == null)
        //{
        //    windowCanvas = activeWindow.AddComponent<Canvas>();
        //    windowCanvas.overrideSorting = true;
        //    windowCanvas.sortingOrder = 100;
        //}

        //if (activeWindow.GetComponent<GraphicRaycaster>() == null)
        //{
        //    activeWindow.AddComponent<GraphicRaycaster>();
        //}

        //List animations
        SetupAnimationButtons(model);
    }

    void SetupAnimationButtons(GameObject model)
    {
        Debug.Log($"Setting up animation buttons for model: {model.name}");

        GameObject tempInstance = Instantiate(model);
        tempInstance.SetActive(false); // Görünmez yap

        Animator animator = tempInstance.GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError($"Model {model.name} has no Animator component!");
            Destroy(tempInstance);
            CloseWindow();
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"Model {model.name} Animator has no Controller assigned!");
            Destroy(tempInstance);
            CloseWindow();
            return;
        }

        //this gets animation lists
        List<AnimationClip> clips = new List<AnimationClip>();
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null) // Null check
                clips.Add(clip);
        }
        Debug.Log($"Found {clips.Count} animation clips");

        Transform buttonParent = activeWindow.transform.Find("ButtonParent");
        if (buttonParent == null)
        {
            Debug.LogError("Content not found in prefab hierarchy!");
            Destroy(tempInstance);
            CloseWindow();
            return;
        }
        Debug.Log($"Button parent: {buttonParent.name}");

        // we will only need this if I somehow mess up in the inspector
        //List<Transform> toDestroy = new List<Transform>();
        //foreach (Transform child in buttonParent)
        //{
        //    if (child.name != "ButtonTemplate")
        //        toDestroy.Add(child);
        //}
        //foreach (Transform child in toDestroy)
        //{
        //    Destroy(child.gameObject);
        //}

        Transform buttonTemplate = buttonParent.Find("ButtonTemplate");
        GameObject buttonPrefab = null;
        //using button prefab as a temp variable to reach buttontemplate

        if (buttonTemplate != null)
        {
            buttonPrefab = buttonTemplate.gameObject;
            buttonPrefab.SetActive(false); // hiding the template
        }
        else
        {
            Debug.LogError("ButtonTemplate not found in prefab! Please ensure ButtonTemplate exists in prefab.");
            Destroy(tempInstance);
            CloseWindow();
            return;
        }

        int buttonIndex = 0;
        foreach (AnimationClip clip in clips)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            buttonObj.name = $"Button_{clip.name}";
            buttonObj.SetActive(true);

            // this doesnt work have some work to deal
            buttonObj.transform.SetSiblingIndex(buttonIndex++);

            Button button = buttonObj.GetComponent<Button>();
            //if (button == null)
            //{
            //    Debug.LogError($"Button component not found on {buttonObj.name}");
            //    continue;
            //}

            //Image buttonImage = buttonObj.GetComponent<Image>();
            //if (buttonImage != null)
            //{
            //    buttonImage.raycastTarget = true;
            //}

            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText == null)
            {
                //Text legacyText = buttonObj.GetComponentInChildren<Text>();
                //if (legacyText != null)
                //{
                //    // Legacy Text'i TMPro'ya dönüştür
                //    GameObject textObj = legacyText.gameObject;
                //    string textContent = legacyText.text;
                //    DestroyImmediate(legacyText);
                //    buttonText = textObj.AddComponent<TextMeshProUGUI>();
                //    buttonText.text = textContent;
                //}
                //else
                //{
                //    Debug.LogError($"No text component found on button {buttonObj.name}");
                //    continue;
                //}
                // we dont need this because legacy text is literally dead for 10 years but I added it for safety
            }

            // arranging text
            string cleanName = GetCleanAnimationName(clip.name);
            buttonText.text = $"{cleanName} ({clip.length:F1}s)";
            buttonText.raycastTarget = false; //just so that we click on the text


            // using local variables so that we can work with a set of buttons
            string animName = clip.name;
            float animLength = clip.length;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                Debug.Log($"Button clicked: {animName}");
                OnAnimationButtonClicked(animName, animLength);
            });
        }

        // adding a cancel button
        GameObject cancelButton = Instantiate(buttonPrefab, buttonParent);
        cancelButton.name = "CancelButton";
        cancelButton.SetActive(true);
        cancelButton.transform.SetAsLastSibling(); // this was unneeded but why not tho

        Button cancel = cancelButton.GetComponent<Button>();
        if (cancel != null)
        {

            Image cancelImage = cancelButton.GetComponent<Image>();
            if (cancelImage != null)
            {
                cancelImage.raycastTarget = true;
                cancelImage.color = new Color(0.8f, 0.3f, 0.3f, 1f);
            }

            TextMeshProUGUI cancelText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelText != null)
            {
                cancelText.text = "Cancel";
                cancelText.raycastTarget = false;
            }

            cancel.onClick.RemoveAllListeners();
            cancel.onClick.AddListener(() => {
                Debug.Log("Cancel button clicked");
                CloseWindow();
            });
        }

        Destroy(tempInstance);

        StartCoroutine(ForceLayoutUpdate(buttonParent));
    }

    System.Collections.IEnumerator ForceLayoutUpdate(Transform buttonParent)
    {
        yield return null; 

        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

        ScrollRect scrollRect = activeWindow.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void OnAnimationButtonClicked(string animationName, float duration)
    {
        Debug.Log($"Selected animation: {animationName} with duration: {duration}");

        onAnimationSelected?.Invoke(animationName, duration);

        CloseWindow();
    }

    // this only works with my set of animations. I work the guy that making those anims for 3 years straight so we might have to change this

    string GetCleanAnimationName(string fullName)
    {
        if (fullName.Contains("|"))
        {
            
            if (fullName.Split('|')[1].Replace("_New", "").Contains("Idel"))
            {
                return "Idle";
            }
            else
            {
                return fullName.Split('|')[1].Replace("_New", "");
            }
        }
        else { return fullName; }
       
    }

    public void CloseWindow()
    {
        if (activeWindow != null)
        {
            Destroy(activeWindow);
            activeWindow = null;
        }
    }

    // debug helper, might not need it. Delete on prod.
    void OnEnable()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("No EventSystem found in the scene! UI interactions won't work.");
            Debug.LogError("Please add an EventSystem: GameObject > UI > Event System");
        }
    }
}