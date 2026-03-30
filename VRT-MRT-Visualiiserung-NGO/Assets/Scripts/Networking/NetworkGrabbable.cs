using Oculus.Interaction;
using Unity.Netcode;

public class NetworkGrabbable : NetworkBehaviour
{
    private Grabbable grabbable;

     void Start() {
        grabbable = GetComponent<Grabbable>();
    }

    void Update() {
        if (grabbable.didStart && !IsOwner) {
            RequestOwnershipServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        GetComponent<NetworkObject>().ChangeOwnership(rpcParams.Receive.SenderClientId);
    }
}