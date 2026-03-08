using System;
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
            ApiClient.Instance.Get<Meeting>(
                "authorization/myCurrentMeeting", 
                async (meeting) =>  {
                    if (meeting != null)
                    {
                        Debug.Log($"Found ID: {meeting.id}");

                        EnterMeeting(meeting);
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

            ApiClient.Instance.Post<AddSessionCodeRequest>(
                $"authorization/addSessionCode",
                new AddSessionCodeRequest { meetingId = meeting.id, sessionCode = joinCode },
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
            /*
             ApiClient.Instance.Get<string>(
                $"authorization/fetchSessionCode?meetingId={meetingId}",
                async (response) => {
                    session.JoinCode = response.ToString();
                    Debug.Log("Fetched join code: " + session.JoinCode);
                    
                    Debug.Log("Starting CLIENT - " + session.NetworkMode);
                    NetworkActions.Instance.joinCodeInput = session.JoinCode;
                    await NetworkActions.Instance.StartClientBasedOnMode(session.NetworkMode);
                },
                (error) => {
                    Debug.LogError("Failed to fetch join code from server: " + error);
                }
            );
            */

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

    private class AddSessionCodeRequest
    {
        public int meetingId;
        public string sessionCode;
    }
}