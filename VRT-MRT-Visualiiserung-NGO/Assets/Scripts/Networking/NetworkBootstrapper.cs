using UnityEngine;

public class NetworkBootstrapper : MonoBehaviour
{
    async void Start()
    {
        var session = SessionManager.Instance;

        if (session == null)
        {
            Debug.LogError("SessionManager instance not found!");
            return;
        }

        if (session.CachedMeeting != null && session.CachedMeeting.id != 0)
        {
            Debug.Log("Using cached meeting data. Skipping network call.");
            EnterMeeting(session.CachedMeeting);
            
            session.CachedMeeting = null; 
        }
        else
        {
            ApiClient.Instance.Get<MeetingApiResponse>(
                "meetings/current",
                async (response) =>  {
                    if (response?.meeting != null)
                    {
                        Debug.Log($"Found ID: {response.meeting.id}");

                        EnterMeeting(response.meeting);
                    }
                },
                (error) => Debug.LogError(error)
            );
        }
    }

    private async void EnterMeeting(Meeting meeting)
    {
        var session = SessionManager.Instance;

        if (session.StartMode == StartMode.Host)
        {
            Debug.Log("Starting HOST - " + session.NetworkMode);
            string joinCode = await NetworkActions.Instance.StartHostBasedOnMode(session.NetworkMode);
            session.JoinCode = joinCode;

            ApiClient.Instance.Put<object>(
                $"meetings/{meeting.id}/session-code",
                new SetSessionCodeRequest { sessionCode = joinCode },
                async
                (response) => {
                    Debug.Log("Join code set successfully on server.");
                },
                (error) => {
                    Debug.LogError("Failed to set join code on server: " + error);
                }
            );
        }
        else if (session.StartMode == StartMode.Client)
        {
            session.JoinCode = meeting.sessionCode;
            Debug.Log("Starting CLIENT - " + session.NetworkMode);
            NetworkActions.Instance.joinCodeInput = session.JoinCode;
            await NetworkActions.Instance.StartClientBasedOnMode(session.NetworkMode);
        }
        else
        {
            Debug.LogError("No start mode set");
        }

    }

    [System.Serializable]
    private class MeetingApiResponse
    {
        public Meeting meeting;
    }

    [System.Serializable]
    private class SetSessionCodeRequest
    {
        public string sessionCode;
    }
}