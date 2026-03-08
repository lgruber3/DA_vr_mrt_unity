using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] 
    private Transform xrRig;
    private Vector3 spawnOffset;
    private bool hasSpawn;

    [ClientRpc]
    public void SetSpawnOffsetClientRpc(Vector3 spawnPosition)
    {
        if (!IsOwner)
            return;

        Debug.Log("Setting spawn to: " + spawnPosition);

        xrRig = FindFirstObjectByType<OVRManager>().transform;

        hasSpawn = true;

        xrRig.SetPositionAndRotation(
            spawnPosition,
            xrRig.rotation
        );
    }
}
