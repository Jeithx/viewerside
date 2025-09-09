using UnityEngine;
using System.Collections.Generic;

public class ModelContent : Content
{
    private GameObject _modelInstance;
    private GameObject _modelPrefab;
    private Animator _animator;
    private string _initialAnimation = "";
    private bool _animationStarted = false;
    private float _animationLength = 0f;

    public GameObject GetModelInstance() => _modelInstance;

    public override void Show()
    {
        if (_modelInstance != null)
        {
            _modelInstance.SetActive(true);
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
        // get anim length
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
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
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

    public void SetAnimationTime(float localTime, bool isPlaying)
    {
        if (_animator == null || string.IsNullOrEmpty(_initialAnimation)) return;

        _animator.enabled = true;
        float normalizedTime = (_animationLength > 0) ? Mathf.Clamp01(localTime / _animationLength) : 0f;

        if (!isPlaying)
        {
            _animator.speed = 0f;
            _animator.Play(_initialAnimation, 0, normalizedTime);
        }
        else
        {
            _animator.speed = 1f;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float currentNormalizedTime = stateInfo.normalizedTime % 1f;
            if (Mathf.Abs(currentNormalizedTime - normalizedTime) > 0.1f)
            {
                _animator.Play(_initialAnimation, 0, normalizedTime);
            }
            _animationStarted = true;
        }
    }

    public void ResetAnimation()
    {
        _animationStarted = false;
        if (_animator != null)
        {
            _animator.enabled = false;
            _animator.speed = 1f;
        }
    }

    public override bool IsModel() { return true; }

    /// <summary>
    /// a basic constructor for modelcontent
    /// </summary>
    public ModelContent(GameObject modelPrefab, float startingPoint, float duration)
    {
        if (!modelPrefab)
        {
            Debug.LogError("ModelContent: modelPrefab null.");
            return;
        }

        _modelPrefab = modelPrefab;
        startTime = startingPoint;
        contentLength = duration;

        _modelInstance = Object.Instantiate(modelPrefab);
        _modelInstance.layer = 8;
        SetLayerRecursively(_modelInstance, 8);

        _modelInstance.transform.localScale = Vector3.one * 3f;
        _modelInstance.transform.rotation = Quaternion.Euler(0, 180, 0);
        PositionModelInScene();

        _animator = _modelInstance.GetComponent<Animator>();
        _modelInstance.SetActive(false);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void UpdateAnimation(float localTime)
    {
        if (_animator != null && _animationStarted)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // Animasyonun bitip bitmediğini kontrol et
            if (stateInfo.normalizedTime >= 1.0f && !stateInfo.loop)
            {
                Hide();
            }
        }
    }

    /// <summary>
    /// this code was in timelinegrid before
    /// </summary>
    private void PositionModelInScene()
    {
        ViewerCore viewerCore = Object.FindObjectOfType<ViewerCore>();
        if (viewerCore != null && viewerCore.displayArea != null)
        {
            Vector3 worldCenter = viewerCore.displayArea.transform.position;
            worldCenter.z = -80f;
            _modelInstance.transform.position = worldCenter;
        }
        else
        {
            _modelInstance.transform.position = new Vector3(0, 0, -80f);
            Debug.LogWarning("ViewerCore or displayArea not found. Positioning model at default coordinates.");
        }
    }

    public string GetInitialAnimationName()
    {
        return _initialAnimation;
    }
}