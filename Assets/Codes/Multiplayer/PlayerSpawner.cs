using UnityEngine;
using Photon.Pun;

/// <summary>
/// Quản lý spawn các player multiplayer vào game scene
/// - Spawn prefab character đúng vị trí
/// - Gán camera cho owner
/// - Tối ưu network bandwidth
/// </summary>
public class PlayerSpawner : MonoBehaviourPun
{
    [Header("Spawn Settings")]
    [SerializeField] private Vector3[] spawnPoints = new Vector3[4]
    {
        new Vector3(-5, 0, 0),  // Player 1
        new Vector3(5, 0, 0),   // Player 2
        new Vector3(-5, 5, 0),  // Player 3
        new Vector3(5, 5, 0)    // Player 4
    };

    [SerializeField] private string playerPrefabName = "Prefabs/MultiplayerCharacter";
    [SerializeField] private bool useRandomSpawnPoints = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraDistance = 10f;

    private int currentSpawnIndex = 0;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Chỉ host mới spawn player
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnPlayersForAllClients();
        }
    }

    /// <summary>Spawn nhân vật cho tất cả player</summary>
    private void SpawnPlayersForAllClients()
    {
        int playerCount = PhotonNetwork.CurrentRoom.Players.Count;
        
        Debug.Log($"[PlayerSpawner] Spawn {playerCount} nhân vật...");

        // Lấy danh sách tất cả player
        var players = PhotonNetwork.CurrentRoom.Players.Values;
        int spawnIndex = 0;

        foreach (var player in players)
        {
            Vector3 spawnPos = useRandomSpawnPoints ? 
                GetRandomSpawnPoint() : 
                spawnPoints[spawnIndex % spawnPoints.Length];

            // Lưu spawn point cho player này
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
            {
                { "spawnIndex", spawnIndex },
                { "spawnPoint", spawnPos }
            };
            player.SetCustomProperties(playerProps);

            spawnIndex++;
        }

        // Gọi RPC để tất cả client spawn character của riêng mình
        photonView.RPC("RPC_SpawnCharacter", RpcTarget.AllBuffered);
    }

    /// <summary>RPC để spawn character của player hiện tại</summary>
    [PunRPC]
    private void RPC_SpawnCharacter()
    {
        // Lấy spawn point từ custom property
        Vector3 spawnPoint = GetMySpawnPoint();
        
        // Network Instantiate - sẽ tạo trên tất cả client
        GameObject playerObj = PhotonNetwork.Instantiate(
            playerPrefabName,
            spawnPoint,
            Quaternion.identity
        );

        Debug.Log($"[PlayerSpawner] Spawned {PhotonNetwork.LocalPlayer.NickName} at {spawnPoint}");

        // Nếu đây là player của chúng ta, gắn camera
        MultiplayerCharacter character = playerObj.GetComponent<MultiplayerCharacter>();
        if (character != null && character.IsOwner)
        {
            AttachCameraToPlayer(playerObj);
        }
    }

    /// <summary>Lấy spawn point của player hiện tại</summary>
    private Vector3 GetMySpawnPoint()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties == null)
            return spawnPoints[0];

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("spawnPoint"))
        {
            return (Vector3)PhotonNetwork.LocalPlayer.CustomProperties["spawnPoint"];
        }

        return spawnPoints[0];
    }

    /// <summary>Gắn camera vào player</summary>
    private void AttachCameraToPlayer(GameObject player)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[PlayerSpawner] Camera.main không tìm thấy!");
            return;
        }

        // Tạo camera follow
        CameraFollow cameraFollow = mainCamera.gameObject.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
        }

        cameraFollow.SetTarget(player.transform);
        Debug.Log($"[PlayerSpawner] Camera gắn vào {player.name}");
    }

    /// <summary>Lấy spawn point ngẫu nhiên</summary>
    private Vector3 GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    /// <summary>Đặt spawn points</summary>
    public void SetSpawnPoints(Vector3[] points)
    {
        if (points.Length == 4)
        {
            spawnPoints = points;
        }
    }
}
