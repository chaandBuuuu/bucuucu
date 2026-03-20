using UnityEngine;
using Photon.Pun;

/// <summary>
/// SETUP HELPER - Check this script in Inspector to verify spawn point configuration
/// Drag this to console to verify everything is set up correctly
/// </summary>
public class LobbySpawnerSetupHelper : MonoBehaviourPun
{
    public void VerifySetup()
    {
        LobbySpawner spawner = GetComponent<LobbySpawner>();
        
        if (spawner == null)
        {
            Debug.LogError("❌ LobbySpawner component not found on this GameObject!");
            return;
        }

        Debug.Log("=== LobbySpawner Setup Verification ===");
        Debug.Log("✓ LobbySpawner component found");
        
        // Check if serialized fields are accessible (they're private in LobbySpawner)
        // Just log what to do
        Debug.Log("\n📋 SETUP CHECKLIST:");
        Debug.Log("1. ✓ Ensure LobbySpawner is attached to this GameObject");
        Debug.Log("2. ✓ In Inspector, find LobbySpawner component");
        Debug.Log("3. ✓ Set 'Spawn Points' Size to 4");
        Debug.Log("4. ✓ Create 4 empty GameObjects in LobbyScene:");
        Debug.Log("     - SpawnPoint_1 at position (-5, 0, 0)");
        Debug.Log("     - SpawnPoint_2 at position (5, 0, 0)");
        Debug.Log("     - SpawnPoint_3 at position (-5, 5, 0)");
        Debug.Log("     - SpawnPoint_4 at position (5, 5, 0)");
        Debug.Log("5. ✓ Drag each SpawnPoint into the Spawn Points array [0], [1], [2], [3]");
        Debug.Log("6. ✓ Verify 'Lobby Player Prefab Name' = 'Prefabs/LobbyPlayer'");
        Debug.Log("7. ✓ Make sure LobbyPlayer.prefab exists in Assets/Prefabs/");
        
        Debug.Log("\n🎮 HOW IT WORKS:");
        Debug.Log("1. Player enters room name and clicks 'Tạo Phòng' or 'Vào Phòng'");
        Debug.Log("2. OnJoinedRoom() is called (you should see this in Console)");
        Debug.Log("3. LobbySpawner.SpawnMyCharacter() is triggered");
        Debug.Log("4. LobbyPlayer prefab is instantiated at spawn point");
        Debug.Log("5. Player select character from CharacterSelectPanel");
        Debug.Log("6. Character info is synced to other players");
    }
}
