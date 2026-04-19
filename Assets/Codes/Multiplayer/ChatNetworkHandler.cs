using UnityEngine;
using Fusion;

/// <summary>
/// ✅ FIXED ChatNetworkHandler:
///   - Kế thừa NetworkBehaviour và được SPAWN như NetworkObject riêng
///   - Không gắn vào NetworkRunner GameObject nữa
///   - Server spawn 1 lần khi scene load (tương tự MultiCameraManager)
///
/// Setup:
///   1. Tạo prefab ChatNetworkHandler (Empty GameObject + NetworkObject + script này)
///   2. Gán vào RacingCarSpawner.chatHandlerPrefab
///   3. Server sẽ tự spawn khi scene load
/// </summary>
public class ChatNetworkHandler : NetworkBehaviour
{
    public static ChatNetworkHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        Debug.Log($"[ChatNetworkHandler] ✅ Spawned – HasInputAuthority={HasInputAuthority}");
    }

    /// <summary>
    /// Gọi từ GameChatManager khi local player gửi tin
    /// </summary>
    public void SendChat(string playerName, string message)
    {
        Debug.Log($"[ChatNetworkHandler] 📡 SendChat called: {playerName}: {message}");
        RPC_Broadcast(playerName, message);
    }

    /// <summary>
    /// ✅ RPC từ bất kỳ client nào → broadcast tới tất cả
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Broadcast(string playerName, string message)
    {
        Debug.Log($"[ChatNetworkHandler] 💬 RPC_Broadcast received: {playerName}: {message}");
        
        if (GameChatManager.Instance != null)
        {
            GameChatManager.Instance.AddMessageLocal(playerName, message);
            Debug.Log($"[ChatNetworkHandler] ✅ Called GameChatManager.AddMessageLocal");
        }
        else
        {
            Debug.LogError("[ChatNetworkHandler] ❌ GameChatManager.Instance is NULL!");
        }
    }
}
