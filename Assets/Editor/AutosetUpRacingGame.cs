#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.IO;

public class RacingGameAutoSetup : EditorWindow
{
    private const string PREFAB_FOLDER = "Assets/Prefabs/RacingGame/";

    [MenuItem("RacingGame/🚀 Auto Setup Racing Scene (GamePlay)")]
    public static void ShowWindow()
    {
        GetWindow<RacingGameAutoSetup>("Racing Auto Setup").minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        GUILayout.Space(15);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUILayout.Label("🏎 AUTO SETUP RACING SCENE (GamePlay)", titleStyle);
        EditorGUILayout.HelpBox("Tự động tạo đầy đủ Scene Game Play theo script bạn cung cấp.\nScene sẽ được đặt tên là GamePlay.", MessageType.Info);
        GUILayout.Space(20);

        if (GUILayout.Button("▶ TẠO TOÀN BỘ SCENE GAMEPLAY", GUILayout.Height(60)))
        {
            RunFullRacingSetup();
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Hoặc setup từng phần:", EditorStyles.boldLabel);

        if (GUILayout.Button("Tạo RaceManager")) CreateRaceManager();
        if (GUILayout.Button("Tạo RacingCarSpawner")) CreateRacingCarSpawner();
        if (GUILayout.Button("Tạo FinishLine + SpawnPoints")) CreateTrackObjects();
        if (GUILayout.Button("Tạo Race UI Canvas")) CreateRaceUICanvas();
        if (GUILayout.Button("Tạo MultiCameraManager")) CreateMultiCameraManager();
        if (GUILayout.Button("Tạo 4 Car Prefabs + CarPrefabList")) CreateCarPrefabs();

        GUILayout.Space(20);
        if (GUILayout.Button("🧹 Cleanup Racing Scene", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xóa hết object racing trong scene hiện tại?", "Xóa", "Hủy"))
                CleanupScene();
        }
    }

    private void RunFullRacingSetup()
    {
        CreatePrefabsFolder();
        CreateCarPrefabs();
        CreateRaceManager();
        CreateTrackObjects();
        CreateRacingCarSpawner();
        CreateRaceUICanvas();
        CreateMultiCameraManager();

        EditorUtility.DisplayDialog("✅ HOÀN TẤT!", 
            "Scene Game Play đã được setup xong!\n\n" +
            "✅ Hãy lưu scene với tên chính xác: GamePlay\n" +
            "Sau đó vào Build Settings → kéo GamePlay vào vị trí Index = 2\n\n" +
            "FusionNetworkManager đã được thiết kế load Index 2.", "OK");

        Debug.Log("<color=green>[RacingAutoSetup] ✅ Scene GamePlay đã được tạo hoàn chỉnh!</color>");
    }

    // ====================== PHẦN CODE CÒN LẠI GIỮ NGUYÊN (không thay đổi) ======================
    private void CreateCarPrefabs()
    {
        CreatePrefabsFolder();
        string[] carNames = { "Car_Hacker", "Car_GhostHunter", "Car_Priest", "Car_Scientist" };
        Color[] colors = { Color.red, Color.green, Color.yellow, new Color(0f, 0.5f, 1f) };

        CarPrefabList list = CreateOrGetCarPrefabList();

        for (int i = 0; i < 4; i++)
        {
            string path = PREFAB_FOLDER + carNames[i] + ".prefab";
            if (File.Exists(path)) continue;

            GameObject go = new GameObject(carNames[i]);
            go.AddComponent<NetworkObject>();
            go.AddComponent<CarController>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            var sr = go.AddComponent<SpriteRenderer>();

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 2.2f);

            sr.color = colors[i];
            sr.sortingOrder = 10;

            go.tag = "Player";

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);

            list.carPrefabs[i] = AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
            EditorUtility.SetDirty(list);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Đã tạo 4 Car Prefabs");
    }

    private CarPrefabList CreateOrGetCarPrefabList()
    {
        string path = "Assets/CarPrefabList.asset";
        CarPrefabList list = AssetDatabase.LoadAssetAtPath<CarPrefabList>(path);
        if (list == null)
        {
            list = ScriptableObject.CreateInstance<CarPrefabList>();
            AssetDatabase.CreateAsset(list, path);
        }
        return list;
    }

    private void CreateRaceManager()
    {
        if (FindAnyObjectByType<RaceManager>() != null) return;
        var go = new GameObject("RaceManager");
        go.AddComponent<NetworkObject>();
        go.AddComponent<RaceManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create RaceManager");
    }

    private void CreateTrackObjects()
    {
        if (GameObject.Find("FinishLine") == null)
        {
            var fl = new GameObject("FinishLine");
            fl.transform.position = new Vector3(0, -15, 0);
            var col = fl.AddComponent<BoxCollider2D>();
            col.size = new Vector2(12, 2);
            col.isTrigger = true;
            fl.AddComponent<FinishLineDetector>();
            Undo.RegisterCreatedObjectUndo(fl, "Create FinishLine");
        }

        if (GameObject.Find("SpawnPoints") == null)
        {
            var container = new GameObject("SpawnPoints");
            Vector3[] pos = { new(-6,-14,0), new(6,-14,0), new(-6,-10,0), new(6,-10,0) };
            for (int i = 0; i < 4; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(container.transform);
                sp.transform.position = pos[i];
            }
            Undo.RegisterCreatedObjectUndo(container, "Create SpawnPoints");
        }
    }

    private void CreateRacingCarSpawner()
    {
        if (FindAnyObjectByType<RacingCarSpawner>() != null) return;

        var go = new GameObject("RacingCarSpawner");
        var spawner = go.AddComponent<RacingCarSpawner>();

        CarPrefabList list = CreateOrGetCarPrefabList();
        var serializedObject = new SerializedObject(spawner);
        var prop = serializedObject.FindProperty("carPrefabList");
        if (prop != null)
        {
            prop.objectReferenceValue = list;
            serializedObject.ApplyModifiedProperties();
        }

        Undo.RegisterCreatedObjectUndo(go, "Create RacingCarSpawner");
        Debug.Log("✅ RacingCarSpawner đã tạo và gán CarPrefabList");
    }

    private void CreateRaceUICanvas()
    {
        if (GameObject.Find("RaceUICanvas") == null)
        {
            var canvasGO = new GameObject("RaceUICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<RaceUI>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create RaceUICanvas");
        }
    }

    private void CreateMultiCameraManager()
    {
        if (FindAnyObjectByType<MultiCameraManager>() != null) return;
        var go = new GameObject("MultiCameraManager");
        go.AddComponent<MultiCameraManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create MultiCameraManager");
    }

    private void CleanupScene()
    {
        string[] objs = { "RaceManager", "FinishLine", "SpawnPoints", "RacingCarSpawner", "RaceUICanvas", "MultiCameraManager" };
        foreach (var name in objs)
        {
            var go = GameObject.Find(name);
            if (go) DestroyImmediate(go);
        }
    }

    private void CreatePrefabsFolder()
    {
        if (!Directory.Exists(PREFAB_FOLDER))
            Directory.CreateDirectory(PREFAB_FOLDER);
    }
}
#endif