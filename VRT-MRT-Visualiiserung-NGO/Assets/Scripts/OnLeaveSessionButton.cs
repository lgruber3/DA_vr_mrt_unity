using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnLeaveSessionButton : MonoBehaviour
{
    public void OnLeaveSession()
    {
        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }
        
        NetworkManager.Singleton.Shutdown();

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.JoinCode = null;
            SessionManager.Instance.CachedMeeting = null;

            SessionManager.Instance.EndSession();
        }

        SceneManager.LoadScene("LoginScene");
    }
}
