using UnityEngine;
using Photon.Pun;

/// <summary>
/// Spawn players khi vào lobby
/// </summary>
public class LobbySpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    [SerializeField] private string lobbyPlayerPrefabName = "LobbyPlayer";

    public override void OnJoinedRoom()
    {
        Debug.Log("[LobbySpawner] Joined room, spawning character...");
        Debug.Log($"[LobbySpawner] Spawn point array length: {spawnPoints.Length}");
        
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[LobbySpawner] ❌ Spawn points array is EMPTY! Set size to 4 in Inspector.");
            return;
        }
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                Debug.LogWarning($"[LobbySpawner] ⚠️ Spawn point [{i}] is NOT assigned!");
            else
                Debug.Log($"[LobbySpawner] ✓ Spawn point [{i}] = {spawnPoints[i].gameObject.name}");
        }

        // Spawn character của player này
        SpawnMyCharacter();
    }

    /// <summary>Spawn character của player hiện tại</summary>
    private void SpawnMyCharacter()
    {
        Vector3 spawnPos = GetMySpawnPoint();

        Debug.Log($"[LobbySpawner] Attempting to spawn prefab: {lobbyPlayerPrefabName}");
        
        GameObject playerObj = PhotonNetwork.Instantiate(
            lobbyPlayerPrefabName,
            spawnPos,
            Quaternion.identity
        );
        
        if (playerObj != null)
            Debug.Log($"[LobbySpawner] ✓ Spawned player at {spawnPos}");
        else
            Debug.LogError($"[LobbySpawner] ❌ Failed to spawn! Prefab not found: {lobbyPlayerPrefabName}");
    }

    /// <summary>Lấy spawn point dựa vào actor number</summary>
    private Vector3 GetMySpawnPoint()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int index = (actorNumber - 1) % spawnPoints.Length;
        
        if (spawnPoints[index] != null)
            return spawnPoints[index].position;
        
        Debug.LogWarning($"[LobbySpawner] Spawn point {index} not found!");
        return Vector3.zero;
    }
}
