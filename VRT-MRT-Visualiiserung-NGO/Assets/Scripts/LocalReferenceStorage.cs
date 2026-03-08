using UnityEngine;

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
}
