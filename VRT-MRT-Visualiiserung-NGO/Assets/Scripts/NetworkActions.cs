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
using Unity.Services.Vivox;
using System;
using UnityEngine.Rendering;

public class NetworkActions : MonoBehaviour
{
    public static NetworkActions Instance;
    public string joinCodeInput = "";
    public static event Action<string> OnConnectionEstablished;

    async void Start()
    {
        Instance = this;

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Unity Services initialized and signed in.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public async Task<string> StartHostBasedOnMode(NetworkMode currentMode)
    {
        string code = "";

        switch (currentMode)
        {

            case NetworkMode.LAN:
                Debug.Log("Starting host in LAN mode...");
                code = await StartHostLan();

                return code;
            case NetworkMode.Relay:
                Debug.Log("Starting host using Relay...");
                code = await StartHostWithRelay();
                
                return code;
            case NetworkMode.Automatic:
            /*
                if (IsLocalNetworkAvailable())
                {
                    Debug.Log("Automatic mode: LAN detected, starting LAN host...");
                    StartHostLan();
                }
                
                else
                {
                */
                Debug.Log("Automatic mode: LAN not detected, using Relay...");
                code = await StartHostWithRelay();
                return code;
            default:
                Debug.LogError("Unknown network mode!");
                return code;
        }
    }

    public async Task StartClientBasedOnMode(NetworkMode currentMode)
    {
        switch (currentMode)
        {
            case NetworkMode.LAN:
                Debug.Log("Starting client in LAN mode...");

                StartClientLan();
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
            /*
                if (IsLocalNetworkAvailable())
                {
                    Debug.Log("Automatic mode: LAN detected, starting LAN client...");
                    StartClientLan();
                }
                else
                {
                */
                    if (string.IsNullOrEmpty(joinCodeInput))
                    {
                        Debug.LogWarning("Please enter a join code for Relay!");
                        return;
                    }
                    Debug.Log("Automatic mode: LAN not detected, using Relay...");
                    JoinWithRelay(joinCodeInput);
                break;
        }
    }

    private async void StartClientLan()
    {
        string lanChannelName = "LocalGame_" + NetworkManager.Singleton.LocalClientId;
        OnConnectionEstablished?.Invoke(lanChannelName);

        NetworkManager.Singleton.StartClient();
    }

    private async Task<string> StartHostLan()
    {
        string lanChannelName = "LocalGame_" + NetworkManager.Singleton.LocalClientId;
        OnConnectionEstablished?.Invoke(lanChannelName);

        NetworkManager.Singleton.StartHost();

        return "no code for LAN required";
    }

    private async Task<string> StartHostWithRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        OnConnectionEstablished?.Invoke(joinCode);
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

        return joinCode;
    }

    private async void JoinWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        OnConnectionEstablished?.Invoke(joinCode);
        Debug.Log("Joined Relay with join code: " + joinCode);

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
