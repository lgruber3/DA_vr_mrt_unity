using Unity.Netcode;

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
