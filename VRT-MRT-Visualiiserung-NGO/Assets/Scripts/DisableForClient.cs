using Unity.Netcode;
using UnityEngine;

public class DisableForClient : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            this.gameObject.SetActive(false);
        }
    }
}
