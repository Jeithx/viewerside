using UnityEngine;

public class SceneCompositor : MonoBehaviour
{
    [Header("Camera Setup")]
    public Camera uiCamera;
    public Camera modelCamera;

    [Header("Layer Settings")]
    public LayerMask modelLayer = 1 << 8;
    public LayerMask uiLayer = 1 << 5;

    void Start()
    {
        SetupCameras();
        //CreateModelLayer();
    }

    void SetupCameras()
    {
        if (uiCamera == null)
            uiCamera = Camera.main;

        if (modelCamera == null)
        {
            GameObject modelCamGO = new GameObject("Model Camera");
            modelCamera = modelCamGO.AddComponent<Camera>();
        }

        SetupUICamera();
        SetupModelCamera();
    }

    void SetupUICamera()
    {
        uiCamera.depth = 0;
        uiCamera.cullingMask = ~modelLayer;
    }

    void SetupModelCamera()
    {
        modelCamera.transform.position = uiCamera.transform.position;
        modelCamera.transform.rotation = uiCamera.transform.rotation;

        modelCamera.depth = 1;
        modelCamera.cullingMask = modelLayer;
        modelCamera.clearFlags = CameraClearFlags.Depth;

        modelCamera.fieldOfView = uiCamera.fieldOfView;
        modelCamera.nearClipPlane = uiCamera.nearClipPlane;
        modelCamera.farClipPlane = uiCamera.farClipPlane;
    }

    //void CreateModelLayer()
    //{
    //    if (string.IsNullOrEmpty(LayerMask.LayerToName(8)))
    //    {
    //        Debug.LogWarning("Please create a layer named 'Models' at index 8 in Project Settings > Tags and Layers");
    //    }
    //}

    public void SyncCameras()
    {
        if (uiCamera != null && modelCamera != null)
        {
            modelCamera.transform.position = uiCamera.transform.position;
            modelCamera.transform.rotation = uiCamera.transform.rotation;
        }
    }
}