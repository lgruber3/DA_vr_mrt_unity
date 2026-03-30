using System;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxManager : MonoBehaviour
{      
    private bool _isJoining;
    [SerializeField]
    private string _currentChannelName;

    private void OnEnable()
    {
        NetworkActions.OnConnectionEstablished += HandleNetworkJoined;
    }

    private void OnDisable()
    {
        NetworkActions.OnConnectionEstablished -= HandleNetworkJoined;
    }

    async void Start()
    {
        #if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
        #endif

        await VivoxService.Instance.InitializeAsync();
        await LoginToVivox();
    }

    private async void HandleNetworkJoined(string channelName)
    {
        _currentChannelName = channelName;
    }

    private async Task LoginToVivox()
    {
        if (VivoxService.Instance.IsLoggedIn) return;

        LoginOptions options = new LoginOptions { DisplayName = "Player_" + UnityEngine.Random.Range(0, 100) };
        await VivoxService.Instance.LoginAsync(options);

        Debug.Log("Logged into Vivox as " + options.DisplayName);
    }

    public async void OnVoiceToggleChanged(bool isOn)
    {
        if (string.IsNullOrEmpty(_currentChannelName))
        {
            Debug.LogWarning("Cannot toggle voice: No active session/Join Code found.");
            return;
        }

        if (isOn)
        {
            await JoinVoice();
        }
        else
        {
            await LeaveVoice();
        }
    }

    private async Task JoinVoice()
    {
        if (_isJoining || VivoxService.Instance.ActiveChannels.ContainsKey(_currentChannelName)) return;

        _isJoining = true;
        try 
        {
            await VivoxService.Instance.JoinGroupChannelAsync(_currentChannelName, ChatCapability.AudioOnly);
            Debug.Log($"Joined Vivox Channel: {_currentChannelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join Vivox: {e.Message}");
        }
        finally 
        {
            _isJoining = false;
        }
    }

    private async Task LeaveVoice()
    {
        await VivoxService.Instance.LeaveAllChannelsAsync();
        Debug.Log("Left Vivox Channel and stopped requests.");
    }

    public void SetVolume(float volume)
    {
        VivoxService.Instance.SetOutputDeviceVolume((int)volume);
        Debug.Log($"Set Vivox volume to {volume}");
    }
}
