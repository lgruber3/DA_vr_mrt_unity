using UnityEngine;
using static OVRHand;

public class PassthroughToggle : MonoBehaviour
{
    public enum GestureMode { PinchAndSweep, ThumbTap }

    [Header("Settings")]
    public GestureMode activeGestureMode = GestureMode.PinchAndSweep;
    public bool isPassthroughActive;

    [Header("References")]
    public OVRHand rightHand;
    public Transform handTransform;
    public Camera xrCamera;
    public Material skyboxMaterial;
    public MeshRenderer floorPlaneRenderer;
    public OVRMicrogestureEventSource microgestureEventSource;

    [Header("Swipe Parameters")]
    [SerializeField] private float swipeDistance = 0.15f; 
    [SerializeField] private float maxSwipeTime = 0.6f;

    private bool swipeConsumed;
    private Vector3 pinchStartPos;
    private float pinchStartTime;

    void OnEnable()
    {
        if (microgestureEventSource != null)
        {
            microgestureEventSource.GestureRecognizedEvent.AddListener(HandleMicroGesture);
        }
    }

    void OnDisable()
    {
        if (microgestureEventSource != null)
        {
            microgestureEventSource.GestureRecognizedEvent.RemoveListener(HandleMicroGesture);
        }
    }

    void Start()
    {
        SetPassthrough(isPassthroughActive);
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if (rightHand == null || handTransform == null) return;

        if (activeGestureMode == GestureMode.PinchAndSweep)
        {
            HandleManualPinchAndSweep();
        }
    }

    // called by ovr event source in scene 
    public void HandleMicroGesture(MicrogestureType gesture)
    {
        if (activeGestureMode != GestureMode.ThumbTap) return;

        if (gesture == MicrogestureType.ThumbTap)
        {
            Debug.Log("Meta Microgesture: Thumb Tap detected!");
            TogglePassthrough();
        }
    }

    private void HandleManualPinchAndSweep()
    {
        bool isPinching = rightHand.GetFingerIsPinching(HandFinger.Index);

        if (isPinching)
        {
            if (pinchStartTime == 0f)
            {
                pinchStartPos = handTransform.position;
                pinchStartTime = Time.time;
                swipeConsumed = false;
            }

            if (!swipeConsumed)
            {
                Vector3 delta = handTransform.position - pinchStartPos;
                float elapsed = Time.time - pinchStartTime;

                if (delta.x > swipeDistance && elapsed <= maxSwipeTime)
                {
                    TogglePassthrough();
                    swipeConsumed = true;
                }
            }
        }
        else
        {
            pinchStartTime = 0f;
            swipeConsumed = false;
        }
    }

    public void TogglePassthrough()
    {
        isPassthroughActive = !isPassthroughActive;
        SetPassthrough(isPassthroughActive);
    }

    void SetPassthrough(bool enabled)
    {
        if (OVRManager.instance != null)
            OVRManager.instance.isInsightPassthroughEnabled = enabled;

        if (enabled)
        {
            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = new Color(0, 0, 0, 0);
            if(floorPlaneRenderer != null) floorPlaneRenderer.enabled = false;
        }
        else
        {
            RenderSettings.skybox = skyboxMaterial;
            xrCamera.clearFlags = CameraClearFlags.Skybox;
            if(floorPlaneRenderer != null) floorPlaneRenderer.enabled = true;
        }
    }
}