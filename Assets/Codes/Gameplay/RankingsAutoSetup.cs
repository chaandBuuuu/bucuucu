using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ✅ Auto Setup Rankings UI:
///   - Tạo RankingsPanel + ScrollView + Content trong GameEndCanvas
///   - Tạo RankingItemPrefab có RankingItemUI component
///   - Gán thẳng vào GameEndChatManager (public SerializeField, không dùng Reflection)
///   - Có thể gọi từ Inspector button hoặc từ Editor menu
///
/// Setup:
///   1. Gắn script này vào bất kỳ GameObject nào trong scene GamePlay
///   2. Gán gameEndChatManager và gameEndCanvas trong Inspector
///   3. Nhấn "Setup Rankings UI" trong Inspector
/// </summary>
public class RankingsAutoSetup : MonoBehaviour
{
    [Header("References (bắt buộc)")]
    [SerializeField] private GameEndChatManager gameEndChatManager;
    [SerializeField] private Canvas             gameEndCanvas;

    [Header("Settings")]
    [SerializeField] private Vector2 panelSize     = new Vector2(600, 350);
    [SerializeField] private Vector2 panelPosition = new Vector2(0, 50);

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ Inspector button hoặc Editor menu
    /// </summary>
    public void SetupRankingsUI()
    {
        if (gameEndChatManager == null)
        {
            Debug.LogError("[RankingsAutoSetup] ❌ gameEndChatManager chưa gán!");
            return;
        }
        if (gameEndCanvas == null)
        {
            Debug.LogError("[RankingsAutoSetup] ❌ gameEndCanvas chưa gán!");
            return;
        }

        Debug.Log("[RankingsAutoSetup] 🚀 Starting Rankings UI setup...");

        // 1. Tạo RankingsPanel trong canvas
        Transform rankingsContainer = CreateRankingsPanel();

        // 2. Tạo RankingItemPrefab
        GameObject rankingItemPrefab = CreateRankingItemPrefab();

        // 3. Gán trực tiếp vào GameEndChatManager (public SerializeField)
        var so = new UnityEngine.Events.UnityEvent(); // dummy — xem bên dưới
        AssignToManager(rankingsContainer, rankingItemPrefab);

        Debug.Log("[RankingsAutoSetup] ✅ Rankings UI setup complete!");

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameEndChatManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────

    private Transform CreateRankingsPanel()
    {
        // Kiểm tra đã có panel chưa
        var existing = gameEndCanvas.transform.Find("RankingsPanel");
        if (existing != null)
        {
            Debug.Log("[RankingsAutoSetup] RankingsPanel đã tồn tại, dùng lại.");
            return existing.Find("ScrollView/Viewport/Content");
        }

        // ── Panel root ────────────────────────────────────────────────────
        var panel = new GameObject("RankingsPanel");
        panel.transform.SetParent(gameEndCanvas.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchoredPosition = panelPosition;
        panelRT.sizeDelta        = panelSize;
        var panelBG = panel.AddComponent<Image>();
        panelBG.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT  = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0, 1);
        titleRT.anchorMax       = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -20);
        titleRT.sizeDelta        = new Vector2(0, 40);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "🏆 BẢNG XẾP HẠNG";
        titleTMP.fontSize  = 24;
        titleTMP.color     = new Color(1f, 0.84f, 0f);
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ── ScrollView ─────────────────────────────────────────────────────
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin       = Vector2.zero;
        scrollRT.anchorMax       = Vector2.one;
        scrollRT.offsetMin       = new Vector2(5, 5);
        scrollRT.offsetMax       = new Vector2(-5, -50);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollBG   = scrollGO.AddComponent<Image>();
        scrollBG.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = Color.clear;
        vpGO.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin       = new Vector2(0, 1);
        contentRT.anchorMax       = new Vector2(1, 1);
        contentRT.pivot           = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = new Vector2(0, 0);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4;
        vlg.padding                = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport   = vpRT;
        scrollRect.content    = contentRT;
        scrollRect.vertical   = true;
        scrollRect.horizontal = false;

        Debug.Log("[RankingsAutoSetup] ✅ RankingsPanel created");
        return contentRT;
    }

    private GameObject CreateRankingItemPrefab()
    {
        // ── Prefab root ───────────────────────────────────────────────────
        var root   = new GameObject("RankingItem");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(0, 48);

        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight  = 48;
        le.flexibleWidth    = 1;

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 0.8f);

        // ── Position Text (#1 / #2 ...) ───────────────────────────────────
        var posGO  = new GameObject("PositionText");
        posGO.transform.SetParent(root.transform, false);
        var posRT  = posGO.AddComponent<RectTransform>();
        posRT.anchorMin       = new Vector2(0, 0.5f);
        posRT.anchorMax       = new Vector2(0, 0.5f);
        posRT.pivot           = new Vector2(0, 0.5f);
        posRT.anchoredPosition = new Vector2(8, 0);
        posRT.sizeDelta        = new Vector2(50, 40);
        var posTMP = posGO.AddComponent<TextMeshProUGUI>();
        posTMP.text      = "#1";
        posTMP.fontSize  = 22;
        posTMP.color     = new Color(1f, 0.84f, 0f);
        posTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Player Name Text ───────────────────────────────────────────────
        var nameGO  = new GameObject("PlayerNameText");
        nameGO.transform.SetParent(root.transform, false);
        var nameRT  = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin       = new Vector2(0, 0);
        nameRT.anchorMax       = new Vector2(1, 1);
        nameRT.offsetMin       = new Vector2(65, 0);
        nameRT.offsetMax       = new Vector2(-110, 0);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text      = "Player Name";
        nameTMP.fontSize  = 18;
        nameTMP.color     = Color.white;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Time Text ──────────────────────────────────────────────────────
        var timeGO  = new GameObject("TimeText");
        timeGO.transform.SetParent(root.transform, false);
        var timeRT  = timeGO.AddComponent<RectTransform>();
        timeRT.anchorMin       = new Vector2(1, 0.5f);
        timeRT.anchorMax       = new Vector2(1, 0.5f);
        timeRT.pivot           = new Vector2(1, 0.5f);
        timeRT.anchoredPosition = new Vector2(-8, 0);
        timeRT.sizeDelta        = new Vector2(100, 40);
        var timeTMP = timeGO.AddComponent<TextMeshProUGUI>();
        timeTMP.text      = "00.00s";
        timeTMP.fontSize  = 18;
        timeTMP.color     = new Color(0.7f, 0.9f, 1f);
        timeTMP.alignment = TextAlignmentOptions.MidlineRight;

        // ── RankingItemUI script (SerializeField, không dùng Reflection) ───
        var rankUI = root.AddComponent<RankingItemUI_AutoSetup>();
        rankUI.SetRefs(posTMP, nameTMP, timeTMP);

        root.SetActive(false); // Prefab ẩn mặc định
        Debug.Log("[RankingsAutoSetup] ✅ RankingItemPrefab created");
        return root;
    }

    private void AssignToManager(Transform rankingsContainer, GameObject rankingItemPrefab)
    {
        if (gameEndChatManager == null) return;

#if UNITY_EDITOR
        // Dùng SerializedObject để gán đúng trong Editor (undo-able)
        var so = new SerializedObject(gameEndChatManager);
        so.FindProperty("rankingsContainer").objectReferenceValue  = rankingsContainer;
        so.FindProperty("rankingItemPrefab").objectReferenceValue  = rankingItemPrefab;
        so.ApplyModifiedProperties();
        Debug.Log("[RankingsAutoSetup] ✅ References assigned via SerializedObject");
#else
        // Runtime fallback (không hoạt động với private SerializeField)
        Debug.LogWarning("[RankingsAutoSetup] Runtime assignment không hoạt động với private fields. Chỉ dùng trong Editor.");
#endif
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Wrapper của RankingItemUI dùng public fields thay vì private SerializeField
/// để RankingsAutoSetup có thể gán mà không cần Reflection.
/// </summary>
public class RankingItemUI_AutoSetup : MonoBehaviour
{
    public TMP_Text positionText;
    public TMP_Text playerNameText;
    public TMP_Text timeText;

    public void SetRefs(TMP_Text pos, TMP_Text name, TMP_Text time)
    {
        positionText   = pos;
        playerNameText = name;
        timeText       = time;
    }

    public void Initialize(int position, string playerName, float finishTime)
    {
        if (positionText != null)
        {
            positionText.text = $"#{position}";
            positionText.color = position switch
            {
                1 => new Color(1f, 0.84f, 0f),
                2 => new Color(0.75f, 0.75f, 0.75f),
                3 => new Color(0.8f, 0.5f, 0.2f),
                _ => Color.white
            };
        }

        if (playerNameText != null) playerNameText.text = playerName;
        if (timeText       != null) timeText.text       = $"{finishTime:F2}s";
    }
}

// ─────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[CustomEditor(typeof(RankingsAutoSetup))]
public class RankingsAutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        var setup = (RankingsAutoSetup)target;
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Setup Rankings UI", GUILayout.Height(40)))
            setup.SetupRankingsUI();
        GUI.backgroundColor = Color.white;
    }
}
#endif