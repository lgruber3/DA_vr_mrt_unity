using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginSpawner : MonoBehaviour
{
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
        StartCoroutine(PositionPlayer());
    }

    private System.Collections.IEnumerator PositionPlayer()
    {
        yield return new WaitForEndOfFrame();

        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            Vector3 headOffsetXZ = rig.centerEyeAnchor.position - rig.transform.position;
            headOffsetXZ.y = 0f;

            rig.transform.position = transform.position - headOffsetXZ;
            rig.transform.rotation = transform.rotation;
            Debug.Log("Player repositioned to: " + transform.position);
        }
    }
}