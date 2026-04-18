using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

/// <summary>
/// ✅ Partial class mở rộng FusionNetworkManager
/// Disable Main Camera của Menu khi load sang GamePlay scene
/// </summary>
public partial class FusionNetworkManager
{
    private const int RACING_SCENE_INDEX = 2;

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        // ✅ Dùng Unity SceneManager thay vì runner.CurrentScene
        Scene activeScene = SceneManager.GetActiveScene();
        int loadedIndex   = activeScene.buildIndex;

        Debug.Log($"[FusionNetworkManager] OnSceneLoadDone – scene: {activeScene.name} (index {loadedIndex})");

        if (loadedIndex == RACING_SCENE_INDEX)
        {
            DisableMenuCamera();
        }
    }

    private void DisableMenuCamera()
    {
        Scene gameplayScene = SceneManager.GetActiveScene();

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            // Camera không thuộc active scene → là camera của Menu/DontDestroyOnLoad → disable
            if (cam.gameObject.scene != gameplayScene)
            {
                cam.enabled = false;

                var al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;

                Debug.Log($"[FusionNetworkManager] 🎥 Disabled camera: {cam.name} (scene: {cam.gameObject.scene.name})");
            }
        }
    }
}