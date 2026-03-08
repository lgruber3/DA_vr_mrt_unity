using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NotesHandler : MonoBehaviour
{
    [SerializeField]
    private string _noteEndpoint = "/notizen";
    public TMP_InputField NotesInputField;

    void Start()
    {
        SessionManager.OnSessionEnd += OnSessionEnd;
    }

    void OnSessionEnd()
    {
        var noteData = new { notes = NotesInputField.text, meetingId = SessionManager.Instance.JoinCode };

        ApiClient.Instance.Post<object>(_noteEndpoint, noteData,
            onSuccess: (response) =>
            {
                Debug.Log("Note successfully sent to backend.");
            },
            onError: (error) =>
            {
                Debug.LogError($"Error sending note to backend: {error}");
            });
    }
}
