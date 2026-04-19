using UnityEngine;
using Fusion;

/// <summary>
/// ✅ NetworkObject để sync vote giữa các clients
/// Setup: Tạo prefab + spawn từ RacingCarSpawner (tương tự ChatNetworkHandler)
/// </summary>
public class GameEndVoteHandler : NetworkBehaviour
{
    public static GameEndVoteHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RegisterVote(int playerId, string voteType)
    {
        GameEndChatManager.Instance?.RegisterRemoteVote(playerId, voteType);
        Debug.Log($"[GameEndVoteHandler] Vote: Player {playerId} → {voteType}");
    }
}
