using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// ✅ AUTO-SETUP: Automatically initializes SessionDiscoveryManager on scene load
/// - Replaces manual scene setup
/// - Creates SessionDiscoveryManager if missing
/// - Configures with proper NetworkRunner prefab
/// - Runs on first scene load
/// </summary>
public class SessionDiscoveryAutoSetup : MonoBehaviour
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private bool autoSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupSessionDiscovery();
        }
    }

    /// <summary>
    /// Auto-initialize SessionDiscoveryManager if not already in scene
    /// </summary>
    public void SetupSessionDiscovery()
    {
        Debug.Log("[SessionDiscoveryAutoSetup] Checking SessionDiscoveryManager...");

        // Check if already exists
        if (SessionDiscoveryManager.Instance != null)
        {
            Debug.Log("[SessionDiscoveryAutoSetup] ✅ SessionDiscoveryManager already exists!");
            return;
        }

        // ✅ Create it automatically
        GameObject discoveryGO = new GameObject("SessionDiscovery");
        SessionDiscoveryManager manager = discoveryGO.AddComponent<SessionDiscoveryManager>();

        Debug.Log("[SessionDiscoveryAutoSetup] ✅ Created SessionDiscoveryManager!");

        // Get NetworkRunner prefab from various sources
        if (networkRunnerPrefab == null)
        {
            networkRunnerPrefab = FindNetworkRunnerPrefab();
        }

        // Configure manager via reflection (since we can't access SerializeFields directly)
        if (networkRunnerPrefab != null)
        {
            // Use reflection to set the discoveryRunnerPrefab field
            var field = typeof(SessionDiscoveryManager)
                .GetField("discoveryRunnerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(manager, networkRunnerPrefab);
                Debug.Log($"[SessionDiscoveryAutoSetup] ✅ Assigned NetworkRunner prefab: {networkRunnerPrefab.name}");
            }
        }
        else
        {
            Debug.LogWarning("[SessionDiscoveryAutoSetup] ⚠️ NetworkRunner prefab not found! Discovery may not work.");
        }
    }

    /// <summary>
    /// Find NetworkRunner prefab from various sources
    /// </summary>
    private NetworkRunner FindNetworkRunnerPrefab()
    {
        // 1. Check if FusionNetworkManager has runnerPrefab
        if (FusionNetworkManager.Instance != null)
        {
            var field = typeof(FusionNetworkManager)
                .GetField("runnerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                var prefab = field.GetValue(FusionNetworkManager.Instance) as NetworkRunner;
                if (prefab != null)
                {
                    Debug.Log("[SessionDiscoveryAutoSetup] Found runner prefab from FusionNetworkManager");
                    return prefab;
                }
            }
        }

        // 2. Search in Resources folder
        NetworkRunner[] runners = Resources.LoadAll<NetworkRunner>("");
        if (runners.Length > 0)
        {
            Debug.Log("[SessionDiscoveryAutoSetup] Found runner prefab in Resources");
            return runners[0];
        }

        // 3. Search in scene (if instance exists)
        NetworkRunner inScene = FindObjectOfType<NetworkRunner>();
        if (inScene != null && inScene.gameObject.scene.name != null)
        {
            Debug.LogWarning("[SessionDiscoveryAutoSetup] Using runner from scene (not ideal for discovery)");
            return inScene;
        }

        return null;
    }
}
