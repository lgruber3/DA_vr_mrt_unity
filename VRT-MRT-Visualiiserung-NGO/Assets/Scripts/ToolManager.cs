using Oculus.Interaction;
using System.Collections;
using UnityEngine;

public class VRToolManager : MonoBehaviour
{
    public enum ToolMode { None, Transform, Draw, Erase, Pointer }
    public ToolMode currentMode;
    public VRDrawer drawer;
    public Grabbable targetGrabbable;
    public OVRHand rightHand;
    public GameObject transformGizmoVisual;
    public VRPointer pointer;

    [Header("Eraser")]
    public float eraseRadius = 5f;
    [Tooltip("Assign a sphere child of the brush tip. It will be scaled to match eraseRadius.")]
    public Transform eraserVisual;

    private bool _isPinching;
    private Vector3 _eraserBaseScale;

    void Start()
    {
        if (eraserVisual != null)
        {
            float diameter = eraseRadius * 2f;
            eraserVisual.localScale = Vector3.one * diameter;
            _eraserBaseScale = eraserVisual.localScale;
            eraserVisual.gameObject.SetActive(false);
        }

        UpdateComponentStates();
    }

    void Update()
    {
        bool currentlyPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (eraserVisual != null && currentMode == ToolMode.Erase)
            eraserVisual.transform.position = drawer.brushTip.position;

        if (currentlyPinching && !_isPinching)
        {
            _isPinching = true;
            ActionStarted();
        }
        else if (currentlyPinching && _isPinching)
        {
            ActionUpdating();
        }
        else if (!currentlyPinching && _isPinching)
        {
            _isPinching = false;
            ActionEnded();
        }
    }

    public void SetTool(int modeIndex)
    {
        currentMode = (ToolMode)modeIndex;
        UpdateComponentStates();
    }

    private void UpdateComponentStates()
    {
        bool isTransform = currentMode == ToolMode.Transform;
        bool isErase = currentMode == ToolMode.Erase;
        bool isPointer = currentMode == ToolMode.Pointer;

        targetGrabbable.enabled = isTransform;
        transformGizmoVisual.SetActive(isTransform);

        if (eraserVisual != null)
            eraserVisual.gameObject.SetActive(isErase);

        if (pointer != null)
            pointer.enabled = isPointer;
    }

    public void ActionStarted()
    {
        if (currentMode == ToolMode.Draw)    
            drawer.StartDrawing();
        else if (currentMode == ToolMode.Erase)   
            TryErase();
        else if (currentMode == ToolMode.Pointer) 
            pointer.ToggleVisuals(true);
    }

    public void ActionUpdating()
    {
        if (currentMode == ToolMode.Draw)  
            drawer.UpdateDrawing();
        else if (currentMode == ToolMode.Erase)
             TryErase();
    }

    public void ActionEnded()
    {
        if (currentMode == ToolMode.Draw)   
             drawer.StopDrawing();
        else if (currentMode == ToolMode.Pointer) 
            pointer.ToggleVisuals(false);
    }

    private void TryErase()
    {
        Collider[] hitColliders = Physics.OverlapSphere(drawer.brushTip.position, eraseRadius);
        foreach (var hit in hitColliders)
        {
            Debug.Log("Hit: " + hit.gameObject.name);
            if (hit.CompareTag("Drawing"))
            {
                var strokeId = hit.gameObject.GetComponent<DrawingStrokeId>();
                if (strokeId != null && NetworkToolSync.Instance != null)
                    NetworkToolSync.Instance.EraseStroke(strokeId.StrokeId);

                Destroy(hit.gameObject);

                if (eraserVisual != null)
                    StartCoroutine(EraserPopAnimation());

                break;
            }
        }
    }

    private IEnumerator EraserPopAnimation()
    {
        float duration = 0.12f;
        float peakScale = 1.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = 1f + (peakScale - 1f) * (1f - Mathf.Abs(t * 2f - 1f));
            eraserVisual.localScale = _eraserBaseScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        eraserVisual.localScale = _eraserBaseScale;
    }
}
