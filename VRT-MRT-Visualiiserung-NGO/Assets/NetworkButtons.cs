using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class NetworkModeSwitcher : MonoBehaviour
{
    public string joinCodeInput = "";

    private enum NetworkMode { LAN, Relay, Automatic }
    private NetworkMode currentMode = NetworkMode.Automatic;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Unity Services initialized and signed in.");
    }

    void OnGUI()
    {
        float buttonWidth = 200;
        float buttonHeight = 50;
        float spacing = 10;
        float startX = 10;
        float startY = 10;

        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Mode: " + currentMode))
        {
            currentMode = (NetworkMode)(((int)currentMode + 1) % 3); 
        }

        if (GUI.Button(new Rect(startX, startY + (buttonHeight + spacing), buttonWidth, buttonHeight), "Start Host"))
        {
            StartHostBasedOnMode();
        }

        if (GUI.Button(new Rect(startX, startY + 2 * (buttonHeight + spacing), buttonWidth, buttonHeight), "Start Client"))
        {
            StartClientBasedOnMode();
        }

        joinCodeInput = GUI.TextField(new Rect(startX, startY + 3 * (buttonHeight + spacing), buttonWidth, buttonHeight), joinCodeInput);
    }

    private void StartHostBasedOnMode()
    {
        switch (currentMode)
        {
            case NetworkMode.LAN:
                Debug.Log("Starting host in LAN mode...");
                NetworkManager.Singleton.StartHost();
                break;
            case NetworkMode.Relay:
                Debug.Log("Starting host using Relay...");
                StartHostWithRelay();
                break;
            case NetworkMode.Automatic:
                if (IsLocalNetworkAvailable())
                {
                    Debug.Log("Automatic mode: LAN detected, starting LAN host...");
                    NetworkManager.Singleton.StartHost();
                }
                else
                {
                    Debug.Log("Automatic mode: LAN not detected, using Relay...");
                    StartHostWithRelay();
                }
                break;
        }
    }

    private void StartClientBasedOnMode()
    {
        switch (currentMode)
        {
            case NetworkMode.LAN:
                Debug.Log("Starting client in LAN mode...");
                NetworkManager.Singleton.StartClient();
                break;
            case NetworkMode.Relay:
                if (string.IsNullOrEmpty(joinCodeInput))
                {
                    Debug.LogWarning("Please enter a join code!");
                    return;
                }
                Debug.Log("Starting client using Relay...");
                JoinWithRelay(joinCodeInput);
                break;
            case NetworkMode.Automatic:
                if (IsLocalNetworkAvailable())
                {
                    Debug.Log("Automatic mode: LAN detected, starting LAN client...");
                    NetworkManager.Singleton.StartClient();
                }
                else
                {
                    if (string.IsNullOrEmpty(joinCodeInput))
                    {
                        Debug.LogWarning("Please enter a join code for Relay!");
                        return;
                    }
                    Debug.Log("Automatic mode: LAN not detected, using Relay...");
                    JoinWithRelay(joinCodeInput);
                }
                break;
        }
    }

    private async void StartHostWithRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log("Share this join code with the client: " + joinCode);

        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        unityTransport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        NetworkManager.Singleton.StartHost();
    }

    private async void JoinWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        unityTransport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();
    }

    private bool IsLocalNetworkAvailable()
    {
        try
        {
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in hostAddresses)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return true;
            }
        }
        catch { }

        return false;
    }
}
