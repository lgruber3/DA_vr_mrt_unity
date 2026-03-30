using UnityEngine;
using UnityEngine.SceneManagement;
using OVR;

public class LocalReferenceStorage : MonoBehaviour
{
    public static LocalReferenceStorage Instance;

    public Camera localVRCamera;
    public OVRHand rightHand;
    public GameObject playerViewWindow;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
    }

    public void RefreshSceneReferences()
    {
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            if (localVRCamera == null)
                localVRCamera = rig.centerEyeAnchor.GetComponent<Camera>();

            if (rightHand == null)
            {
                foreach (var h in rig.GetComponentsInChildren<OVRHand>())
                {
                if (h.GetHand() == OVRPlugin.Hand.HandRight)
                    {
                        rightHand = h;
                        break;
                    }
                }
            }
        }
    }
}
