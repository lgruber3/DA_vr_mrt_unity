using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerViewWindow : NetworkBehaviour
{
    public Camera spectatorCamera;
    public Camera localVRCamera;
    public GameObject loadingIcon;
    private RawImage windowRawImage;
    private OVRHand rightHand;

    RenderTexture rt;
    NetworkHeadPose target;

    const float FIND_INTERVAL = 1.5f;
    const float ROTATION_SPEED = 200f;
    private bool isFullscreen = false;
    private int originalCullingMask;

    public override void OnNetworkSpawn()
    {
        if (LocalReferenceStorage.Instance == null)
        {
            Debug.LogError("[PlayerViewWindow] LocalReferenceStorage instance is missing.");
            return;
        }

        LocalReferenceStorage.Instance.RefreshSceneReferences();

        GameObject windowObj = LocalReferenceStorage.Instance.playerViewWindow;
        if (windowObj != null)
        {
            windowRawImage = windowObj.GetComponent<RawImage>();
            if (windowRawImage != null) windowRawImage.enabled = false;
            windowObj.GetComponent<Button>().onClick.AddListener(ToggleFullscreen);
        }
        else
        {
            Debug.LogWarning("[PlayerViewWindow] playerViewWindow reference is null. " +
                "Add the PlayerViewWindowRegistrar component to the UI window GameObject in the scene.");
        }

        loadingIcon = GameObject.FindGameObjectWithTag("LoadingIcon");

        if (IsOwner)
        {
            localVRCamera = LocalReferenceStorage.Instance.localVRCamera;
            rightHand = LocalReferenceStorage.Instance.rightHand;

            spectatorCamera.gameObject.SetActive(false);
            if (loadingIcon) loadingIcon.SetActive(true);

            InvokeRepeating(nameof(TryFindTarget), 0.5f, FIND_INTERVAL);
        }
        else
        {
            spectatorCamera.enabled = false;
            this.enabled = false; 
        }
    }

    void TryFindTarget()
    {
        var allPoses = FindObjectsByType<NetworkHeadPose>(FindObjectsSortMode.None);
        
        foreach (var pose in allPoses)
        {
            if (!pose.IsOwner) 
            {
                target = pose;
                SetupCamera();
                CancelInvoke(nameof(TryFindTarget));
                return;
            }
        }
    }

    void SetupCamera()
    {
        if (windowRawImage == null && LocalReferenceStorage.Instance != null)
        {
            GameObject windowObj = LocalReferenceStorage.Instance.playerViewWindow;
            if (windowObj != null)
            {
                windowRawImage = windowObj.GetComponent<RawImage>();
                windowObj.GetComponent<Button>()?.onClick.AddListener(ToggleFullscreen);
            }
        }

        rt = new RenderTexture(512, 512, 16);
        spectatorCamera.targetTexture = rt;

        if (windowRawImage != null)
        {
            windowRawImage.texture = rt;
            windowRawImage.enabled = true;
            windowRawImage.gameObject.SetActive(true);
        }

        spectatorCamera.gameObject.SetActive(true);
        if (loadingIcon) loadingIcon.SetActive(false);
    }

    void Update()
    {
        if (IsOwner && target == null && loadingIcon != null)
        {
            loadingIcon.transform.Rotate(0, 0, -ROTATION_SPEED * Time.deltaTime);
        }

       if (IsPinching(OVRHand.Hand.HandLeft) || IsPinching(OVRHand.Hand.HandRight))
        {
            ToggleFullscreen();
        }
    }

    void LateUpdate()
    {
        if (!IsOwner || target == null) return;

        spectatorCamera.transform.SetPositionAndRotation(
            target.headPos.Value,
            target.headRot.Value
        );
    }

    bool IsPinching(OVRHand.Hand handType)
    {
        if (rightHand != null && rightHand.IsTracked)
        {
            return rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }
        return false;
    }

    public void ToggleFullscreen()
    {
        // remove if feature should be added back, not ideal for vr, causes motion sickness
        return;

        if (target == null) return;

        isFullscreen = !isFullscreen;

        if (isFullscreen)
        {
            originalCullingMask = localVRCamera.cullingMask;
            localVRCamera.cullingMask = 0;

            spectatorCamera.targetTexture = null; 
            spectatorCamera.depth = localVRCamera.depth + 10;
            
            spectatorCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        }
        else
        {
            localVRCamera.cullingMask = originalCullingMask;
            spectatorCamera.targetTexture = rt;
            spectatorCamera.depth = localVRCamera.depth - 1;
            
            spectatorCamera.stereoTargetEye = StereoTargetEyeMask.None; 
        }
    }
}