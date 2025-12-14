using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Core;

public class NetworkButtons : MonoBehaviour
{
    public string joinCodeInput = ""; 

    async void Start()
    {
        await UnityServices.InitializeAsync();

        Debug.Log("Unity Services initialized ");
    }
    
    void OnGUI()
    {
        float buttonWidth = 200;
        float buttonHeight = 50;
        float spacing = 10;
        float startX = 10;
        float startY = 10;

        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Start Host"))
        {
            StartHostButtonPressed();
        }

        if (GUI.Button(new Rect(startX, startY + (buttonHeight + spacing), buttonWidth, buttonHeight), "Start Client"))
        {
            StartClientButtonPressed();
        }

        joinCodeInput = GUI.TextField(new Rect(startX, startY + 2 * (buttonHeight + spacing), buttonWidth, buttonHeight), joinCodeInput);
    }

    private async void StartHostButtonPressed()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

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

    private async void StartClientButtonPressed()
    {
        if (string.IsNullOrEmpty(joinCodeInput))
        {
            Debug.LogWarning("Please enter a join code!");
            return;
        }

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput);

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
}
