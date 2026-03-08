using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Oculus.Platform.Models;
using UnityEngine.UI;
using Unity.Collections;


public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    [SerializeField]
    private string _apiEndpoint = "endpoint";
    [SerializeField]
    private string _accessCode;

    public Button loginButton;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LoginScene")
        {
            GameObject btnObj = GameObject.Find("LoginButton"); 
            if (btnObj != null)
            {
                loginButton = btnObj.GetComponent<Button>();
                loginButton.onClick.RemoveAllListeners();
                loginButton.onClick.AddListener(Login);
            }
        }
    }

    public async void Login()
    {
        loginButton.interactable = false;

        string urlWithParam = $"{_apiEndpoint}/{UnityWebRequest.EscapeURL(_accessCode)}";
        using var request = UnityWebRequest.Get(urlWithParam);

        ApiClient.Instance.Get<LoginResponse>(
            urlWithParam,
            (response) => {
                if (response == null || response.user == null) 
                {
                    Debug.LogError("Server sent a success code, but data is empty");
                    loginButton.interactable = true;
                    ErrorDialog.Instance.Show("Invalider Beitrittscode");
                    return; 
                }

                SessionManager.Instance.JoinCode = _accessCode;
                SessionManager.Instance.NetworkMode = NetworkMode.Relay;

                UserDataManager.Instance.age = response.user.alter;
                UserDataManager.Instance.svNumber = response.user.svNummer;
                UserDataManager.Instance.suspicion = response.user.verdacht;
                UserDataManager.Instance.diagnosis = response.user.diagnose;
                UserDataManager.Instance.id = response.user.id;
                UserDataManager.Instance.firstName = response.user.vorname;
                UserDataManager.Instance.lastName = response.user.nachname;
                UserDataManager.Instance.email = response.user.eMail;
                UserDataManager.Instance.bearerToken = response.token;

                if (response.isPatient)
                {
                    UserDataManager.Instance.userType = UserType.Patient;
                    SessionManager.Instance.StartMode = StartMode.Client;

                    CheckMeetingAndTransition();
                }
                else
                {
                    UserDataManager.Instance.userType = UserType.Doctor;
                    SessionManager.Instance.StartMode = StartMode.Host;

                    SceneManager.LoadScene("MultiplayerScene");
                }
            },
            (error) => {
                Debug.LogError("Beitritt fehlgeschlagen: " + error);
                loginButton.interactable = true;

                string friendlyMessage = ParseError(error);
                ErrorDialog.Instance.Show(friendlyMessage);
            }
        );
    }

    private void CheckMeetingAndTransition()
    {
        ApiClient.Instance.Get<Meeting>(
            "authorization/myCurrentMeeting", 
            (meeting) => {
                if (meeting != null && !string.IsNullOrEmpty(meeting.sessionCode))
                {
                    SessionManager.Instance.CachedMeeting = meeting;
                    SceneManager.LoadScene("MultiplayerScene");
                }
                else
                {
                    loginButton.interactable = true;
                    ErrorDialog.Instance.Show("Der Arzt ist dem Termin noch nicht beigetreten. Bitte warte einen Moment.");
                }
            },
            (error) => {
                loginButton.interactable = true;
                ErrorDialog.Instance.Show("Fehler beim Abrufen der Sitzungsdaten: " + error);
            }
        );
    }

    private string ParseError(string error)
    {
        if (error.Contains("404"))
            return "Ungültiger Zugangscode. Bitte überprüfe deine Eingabe.";
        else if (error.Contains("500"))
            return "Serverfehler. Bitte versuche es später erneut.";
        else
            return "Ein Fehler ist aufgetreten: " + error;
    }

    public void SetAccessCode(string accessCode) {
        _accessCode = accessCode;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public User user;
        public bool isPatient;
        public string token;
    }

    [System.Serializable]
    private class User
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