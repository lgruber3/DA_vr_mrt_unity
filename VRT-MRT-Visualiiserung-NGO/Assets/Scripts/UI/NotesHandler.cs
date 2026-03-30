using System;
using TMPro;
using UnityEngine;

public class NotesHandler : MonoBehaviour
{
    public TMP_InputField NotesInputField;

    private int _meetingId = 0;

    void Start()
    {
        SessionManager.OnSessionEnd += OnSessionEnd;

        ApiClient.Instance.Get<MeetingApiResponse>(
            "meetings/current",
            (response) => {
                if (response?.meeting != null)
                    _meetingId = response.meeting.id;
            },
            (error) => Debug.LogWarning("NotesHandler: could not fetch meeting ID: " + error)
        );
    }

    void OnSessionEnd()
    {
        if (_meetingId == 0)
        {
            Debug.LogWarning("NotesHandler: meeting ID unknown, skipping note submission.");
            return;
        }

        var noteData = new NoteRequest { notes = NotesInputField.text };

        ApiClient.Instance.Patch<object>(
            $"meetings/{_meetingId}/notes",
            noteData,
            onSuccess: (_) => Debug.Log("Note successfully sent to backend."),
            onError: (error) => Debug.LogError($"Error sending note to backend: {error}")
        );
    }

    [Serializable]
    private class MeetingApiResponse
    {
        public Meeting meeting;
    }

    [Serializable]
    private class NoteRequest
    {
        public string notes;
    }
}
