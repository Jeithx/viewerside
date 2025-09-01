using UnityEngine;
using System.Collections.Generic;

public class ModelContent : Content
{
    private GameObject _modelInstance;
    private GameObject _modelPrefab;
    private bool _barBuilt = false;
    private Animator _animator;
    private string _initialAnimation = "";
    private bool _animationStarted = false;
    private float _animationLength = 0f;
    private AnimatorStateInfo _currentStateInfo;
    public RectTransform BarRT { get; private set; }


    public GameObject GetModelInstance() => _modelInstance;

    public override void Show()
    {
        if (_modelInstance != null)
        {
            _modelInstance.SetActive(true);

            // Start with the animation if it is set
            if (!_animationStarted && !string.IsNullOrEmpty(_initialAnimation))
            {
                PlayInitialAnimation();
                _animationStarted = true;
            }
        }
    }

    public override void Hide()
    {
        if (_modelInstance != null)
        {
            _modelInstance.SetActive(false);
            StopAnimation();
        }
    }

    public void SetInitialAnimation(string animationName)
    {
        _initialAnimation = animationName;
        Debug.Log($"Initial animation set to: {animationName}");

        // Get animation length
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                {
                    _animationLength = clip.length;
                    break;
                }
            }
        }
    }

    private void PlayInitialAnimation()
    {
        if (_animator == null)
            _animator = _modelInstance.GetComponent<Animator>();

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            Debug.Log($"Playing initial animation: {_initialAnimation}");
            _animator.enabled = true;
            _animator.Play(_initialAnimation, 0, 0f);
        }
    }

    private void StopAnimation()
    {
        if (_animator != null)
        {
            _animator.enabled = false;
            _animationStarted = false;
        }
    }

    // NEW: Scrubbing support - set animation to specific time
    public void SetAnimationTime(float localTime, bool isPlaying)
    {
        if (_animator == null)
            _animator = _modelInstance.GetComponent<Animator>();

        if (_animator != null && !string.IsNullOrEmpty(_initialAnimation))
        {
            _animator.enabled = true;

            // Calculate normalized time (0-1)
            float normalizedTime = 0f;
            if (_animationLength > 0)
            {
                normalizedTime = Mathf.Clamp01(localTime / _animationLength);
            }

            // If scrubbing (not playing), pause the animation at the current frame
            if (!isPlaying)
            {
                _animator.speed = 0f;
                _animator.Play(_initialAnimation, 0, normalizedTime);
                Debug.Log($"Scrubbing to normalized time: {normalizedTime:F3} (local: {localTime:F2}s)");
            }
            else
            {
                // If playing, check if we need to sync the animation
                _animator.speed = 1f;

                // Only resync if the time difference is significant
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                float currentNormalizedTime = stateInfo.normalizedTime % 1f;
                float timeDifference = Mathf.Abs(currentNormalizedTime - normalizedTime);

                if (timeDifference > 0.1f) // 10% tolerance
                {
                    _animator.Play(_initialAnimation, 0, normalizedTime);
                    Debug.Log($"Resyncing animation to time: {normalizedTime:F3}");
                }

                _animationStarted = true;
            }
        }
    }

    public void UpdateAnimation(float localTime)
    {
        // Hide the model if the animation is finished (non-looped)
        if (_animator != null && _animationStarted)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // Check if animation is finished
            if (stateInfo.normalizedTime >= 1.0f && !stateInfo.loop)
            {
                Debug.Log("Animation finished, hiding model");
                Hide();
            }
        }
    }

    public void ResetAnimation()
    {
        _animationStarted = false;
        if (_animator != null)
        {
            _animator.enabled = false;
            _animator.speed = 1f; // Reset speed to normal
        }
    }

    public override bool IsModel() { return true; }

    /// Constructor of ModelContent
    public ModelContent(
        GameObject modelPrefab,
        GameObject imagePrefab,
        int timeToPixel,
        GameObject contextMenuPrefab,
        RectTransform parentRow,
        float startingPoint,
        float duration)
    {
        if (!modelPrefab || !imagePrefab || !parentRow)
        {
            Debug.LogError("ModelContent: missing prefab(s) or parent.");
            return;
        }

        _modelPrefab = modelPrefab;
        startTime = startingPoint;
        contentLength = duration;

        _modelInstance = Object.Instantiate(modelPrefab);
        _modelInstance.layer = 8; // Models layer
        SetLayerRecursively(_modelInstance, 8);
        _modelInstance.transform.localScale = Vector3.one* 3f;
        _modelInstance.transform.rotation = Quaternion.Euler(0, 180, 0);
        PositionModelAtRawImageCenter();

        _animator = _modelInstance.GetComponent<Animator>();
        _modelInstance.SetActive(false);

        CreateTimelineBar(imagePrefab, timeToPixel, parentRow);
    }
    private void CreateTimelineBar(GameObject imagePrefab, int timeToPixel, RectTransform parentRow)
    {
        if (_barBuilt) return;
        _barBuilt = true;

        var barRoot = Object.Instantiate(imagePrefab);
        // This now works because parentRow is a RectTransform
        barRoot.transform.SetParent(parentRow, false);

        RectTransform rt = barRoot.transform.childCount > 0
            ? barRoot.transform.GetChild(0).GetComponent<RectTransform>()
            : barRoot.GetComponent<RectTransform>();

        if (!rt)
        {
            Debug.LogWarning("ModelContent: bar has no RectTransform.");
            return;
        }

        this.BarRT = rt;
            
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        float width = (contentLength / 4f) * timeToPixel;
        float posX = (startTime / 4f) * timeToPixel;

        rt.anchoredPosition = new Vector2(posX, rt.anchoredPosition.y);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
        rt.ForceUpdateRectTransforms();

        TimelineHoverEffect hoverEffect = rt.gameObject.AddComponent<TimelineHoverEffect>();
        hoverEffect.SetContentInfo("Model", _modelPrefab.name);
        hoverEffect.SetHoverColor(Color.cyan);
        hoverEffect.SetModelContent(this);

        if (!string.IsNullOrEmpty(_initialAnimation))
        {
            hoverEffect.SetContentInfo("Model", $"{_modelPrefab.name} - {GetCleanAnimationName(_initialAnimation)}");
        }
    }

    private string GetCleanAnimationName(string fullName)
    {
        if (fullName.Contains("|"))
            return fullName.Split('|')[1].Replace("_New", "");
        return fullName;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void PositionModelAtRawImageCenter()
    {
        TimelineGrid timeline = Object.FindObjectOfType<TimelineGrid>();
        if (timeline != null && timeline.playbackStackRoot != null)
        {
            Vector3 worldCenter = timeline.playbackStackRoot.transform.position;
            worldCenter.z = -80f;
            _modelInstance.transform.position = worldCenter;
        }
        else
        {
            Debug.LogWarning("TimelineGrid or playbackSurface not found, cannot position model.");
        }
    }

    public string GetInitialAnimationName()
    {
        return _initialAnimation;
    }
}