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
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
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

        Transform spawn = GetNextSpawnPoint();
        StartCoroutine(AssignSpawnWhenReady(clientId, spawn));
    }

    private System.Collections.IEnumerator AssignSpawnWhenReady(ulong clientId, Transform spawn)
    {
        float timeout = 2f;
        while (timeout > 0f)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                client.PlayerObject != null)
            {
                var spawner = client.PlayerObject.GetComponent<PlayerSpawner>();
                if (spawner != null)
                {
                    spawner.SetSpawnOffsetClientRpc(spawn.position);
                    yield break;
                }
            }
            timeout -= Time.deltaTime;
            yield return null;
        }
        Debug.LogWarning($"[SpawnManager] Timed out waiting for player object of client {clientId}");
    }
}
