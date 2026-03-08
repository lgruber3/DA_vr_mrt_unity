using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;
    public int age;
    public int svNumber;
    public string suspicion;
    public string diagnosis;
    public string id;
    public string firstName;
    public string lastName;
    public string email;
    public UserType userType;
    public string bearerToken;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SessionManager.OnSessionEnd += ClearUserData;
    }

    private void ClearUserData()
    {
        age = 0;
        svNumber = 0;
        suspicion = string.Empty;
        diagnosis = string.Empty;
        id = string.Empty;
        firstName = string.Empty;
        lastName = string.Empty;
        email = string.Empty;
        userType = default;
        bearerToken = string.Empty;
    }
}

public enum UserType
{
    Patient,
    Doctor,
    Admin
}