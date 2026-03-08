using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private List<Transform> spawnPoints;
    private int spawnIndex;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private Transform GetNextSpawnPoint()
    {
        var spawn = spawnPoints[spawnIndex];
        spawnIndex = (spawnIndex + 1) % spawnPoints.Count;
        return spawn;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var player = client.PlayerObject;
        if (player == null)
            return;

        Transform spawn = GetNextSpawnPoint();

        player
            .GetComponent<PlayerSpawner>()
            .SetSpawnOffsetClientRpc(spawn.position);
    }
}
