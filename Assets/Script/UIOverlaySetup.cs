using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIOverlaySetup : MonoBehaviour
{
    [Header("Mode")]
    public bool useScreenSpaceOverlay = true;

    [Header("Sorting")]
    public int sortingOrder = 500; // high enough to be above prompts/world-space UI

    [Header("Camera (optional)")]
    public Camera uiCamera; // assign if using Screen Space - Camera

    void Awake()
    {
        var canvas = GetComponent<Canvas>();

        if (useScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
        }
        else
        {
            // Screen Space - Camera setup
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (uiCamera == null && Camera.main != null) uiCamera = Camera.main;
            canvas.worldCamera = uiCamera;
            canvas.sortingOrder = sortingOrder;

            // Ensure UI camera draws above world
            if (uiCamera != null) uiCamera.depth = 10f;
        }
    }
}
