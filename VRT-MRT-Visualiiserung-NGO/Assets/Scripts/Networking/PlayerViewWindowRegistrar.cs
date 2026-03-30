using UnityEngine;

public class PlayerViewWindowRegistrar : MonoBehaviour
{
    private void Start()
    {
        if (LocalReferenceStorage.Instance != null)
            LocalReferenceStorage.Instance.playerViewWindow = gameObject;
    }
}
