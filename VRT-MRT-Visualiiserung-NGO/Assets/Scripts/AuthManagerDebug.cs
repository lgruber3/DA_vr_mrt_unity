using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Oculus.Platform.Models;


public class AuthManagerDebug : MonoBehaviour
{
    public static AuthManagerDebug Instance;

    [SerializeField]
    private string _apiEndpoint = "endpoint";
    [SerializeField]
    private string _accessCode;
    [SerializeField]
    private NetworkMode _networkMode = NetworkMode.Automatic;

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
    public async void LoginJoin() {
        string urlWithParam = $"{_apiEndpoint}?code={UnityWebRequest.EscapeURL(_accessCode)}";
        using var request = UnityWebRequest.Get(urlWithParam);

        ApiClient.Instance.Get<LoginResponse>(
            urlWithParam,
            (response) => {
                SessionManager.Instance.JoinCode = _accessCode;
                SessionManager.Instance.StartMode = StartMode.Client;
                SessionManager.Instance.NetworkMode = _networkMode;

                UserDataManager.Instance.age = response.alter;
                UserDataManager.Instance.svNumber = response.svNummer;
                UserDataManager.Instance.suspicion = response.verdacht;
                UserDataManager.Instance.diagnosis = response.diagnose;
                UserDataManager.Instance.id = response.id;
                UserDataManager.Instance.firstName = response.vorname;
                UserDataManager.Instance.lastName = response.nachname;
                UserDataManager.Instance.email = response.eMail;

                SceneManager.LoadScene("MultiplayerScene");
            },
            (error) => {
                Debug.LogError("Login failed: " + error);
            }
        );
    }

    public async void LoginHost() {
        string urlWithParam = $"{_apiEndpoint}?code={UnityWebRequest.EscapeURL(_accessCode)}";
        using var request = UnityWebRequest.Get(urlWithParam);

        ApiClient.Instance.Get<LoginResponse>(
            urlWithParam,
            (response) => {
                SessionManager.Instance.JoinCode = _accessCode;
                SessionManager.Instance.StartMode = StartMode.Host;
                SessionManager.Instance.NetworkMode = _networkMode;

                UserDataManager.Instance.age = response.alter;
                UserDataManager.Instance.svNumber = response.svNummer;
                UserDataManager.Instance.suspicion = response.verdacht;
                UserDataManager.Instance.diagnosis = response.diagnose;
                UserDataManager.Instance.id = response.id;
                UserDataManager.Instance.firstName = response.vorname;
                UserDataManager.Instance.lastName = response.nachname;
                UserDataManager.Instance.email = response.eMail;

                SceneManager.LoadScene("MultiplayerScene");
            },
            (error) => {
                Debug.LogError("Login failed: " + error);
            }
        );
    }

    public void SetAccessCode(string accessCode) {
        _accessCode = accessCode;
    }

    public void SetNetworkMode(int mode) {
        _networkMode = (NetworkMode)mode;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public int alter;
        public int svNummer;
        public string verdacht;
        public string diagnose;
        public string id;
        public string vorname;  
        public string nachname;
        public string eMail;
    }
}