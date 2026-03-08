using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    public StartMode StartMode;
    public string JoinCode;
    public NetworkMode NetworkMode;
    public Meeting CachedMeeting = null;

    public delegate void SessionEndHandler();
    public static event SessionEndHandler OnSessionEnd;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void EndSession()
    {
        OnSessionEnd?.Invoke();
    }
}

public enum StartMode { None, Host, Client }

public  enum NetworkMode { LAN, Relay, Automatic }
