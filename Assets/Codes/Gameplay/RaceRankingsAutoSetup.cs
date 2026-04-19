using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ✅ Auto Setup Race Rankings Display UI:
///   - Tạo RankingsPanel + ScrollView + Content trên RaceUI Canvas
///   - Tạo RankingItemPrefab với TextMeshProUGUI
///   - Gán thẳng vào RaceRankingsDisplay
///
/// Setup:
///   1. Gắn script này vào bất kỳ GameObject nào trong scene GamePlay
///   2. Gán raceRankingsDisplay và raceUICanvas trong Inspector
///   3. Nhấn "Setup Race Rankings UI" trong Inspector
/// </summary>
public class RaceRankingsAutoSetup : MonoBehaviour
{
    [Header("References (bắt buộc)")]
    [SerializeField] private RaceRankingsDisplay raceRankingsDisplay;
    [SerializeField] private Canvas               raceUICanvas;

    [Header("Settings")]
    [SerializeField] private Vector2 panelSize     = new Vector2(500, 400);
    [SerializeField] private Vector2 panelPosition = new Vector2(-250, -150);

    // ✅ Lưu references buttons để assign vào manager
    private Button _restartButton;
    private Button _menuButton;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ Inspector button hoặc Editor menu
    /// </summary>
    public void SetupRaceRankingsUI()
    {
        if (raceRankingsDisplay == null)
        {
            Debug.LogError("[RaceRankingsAutoSetup] ❌ raceRankingsDisplay chưa gán!");
            return;
        }
        if (raceUICanvas == null)
        {
            Debug.LogError("[RaceRankingsAutoSetup] ❌ raceUICanvas chưa gán!");
            return;
        }

        Debug.Log("[RaceRankingsAutoSetup] 🚀 Starting Race Rankings UI setup...");

        // 1. Tạo RankingsPanel trong canvas
        Transform rankingsContainer = CreateRankingsPanel();

        // 2. Tạo RankingItemPrefab
        GameObject rankingItemPrefab = CreateRankingItemPrefab();

        // 3. Gán vào RaceRankingsDisplay
        AssignToDisplay(rankingsContainer, rankingItemPrefab);

        Debug.Log("[RaceRankingsAutoSetup] ✅ Race Rankings UI setup complete!");

#if UNITY_EDITOR
        EditorUtility.SetDirty(raceRankingsDisplay);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────

    private Transform CreateRankingsPanel()
    {
        // Kiểm tra đã có panel chưa
        var existing = raceUICanvas.transform.Find("RaceRankingsPanel");
        if (existing != null)
        {
            Debug.Log("[RaceRankingsAutoSetup] RaceRankingsPanel đã tồn tại, dùng lại.");
            var content = existing.Find("ScrollView/Viewport/Content");
            if (content != null) return content;
        }

        // ── Panel root ────────────────────────────────────────────────────
        var panel = new GameObject("RaceRankingsPanel");
        panel.transform.SetParent(raceUICanvas.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchoredPosition = panelPosition;
        panelRT.sizeDelta        = panelSize;

        // Background Image
        var panelBG = panel.AddComponent<Image>();
        panelBG.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        // CanvasGroup (để toggle)
        var canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // ── Title ──────────────────────────────────────────────────────────
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT  = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0, 1);
        titleRT.anchorMax       = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -15);
        titleRT.sizeDelta        = new Vector2(0, 30);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "📊 BẢNG XẾP HẠNG";
        titleTMP.fontSize  = 20;
        titleTMP.color     = new Color(1f, 0.84f, 0f);
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ── ScrollView ─────────────────────────────────────────────────────
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin       = Vector2.zero;
        scrollRT.anchorMax       = Vector2.one;
        scrollRT.offsetMin       = new Vector2(5, 45);  // ✅ Dành chỗ cho buttons
        scrollRT.offsetMax       = new Vector2(-5, -5);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollBG   = scrollGO.AddComponent<Image>();
        scrollBG.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
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
        vlg.spacing                = 2;
        vlg.padding                = new RectOffset(5, 5, 5, 5);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport   = vpRT;
        scrollRect.content    = contentRT;
        scrollRect.vertical   = true;
        scrollRect.horizontal = false;

        // ✅ Tạo Buttons Container (Restart + Menu)
        CreateButtonsContainer(panel);

        Debug.Log("[RaceRankingsAutoSetup] ✅ RaceRankingsPanel created");
        return contentRT;
    }

    private void CreateButtonsContainer(GameObject panel)
    {
        var buttonsGO = new GameObject("ButtonsContainer");
        buttonsGO.transform.SetParent(panel.transform, false);
        var buttonsRT = buttonsGO.AddComponent<RectTransform>();
        buttonsRT.anchorMin       = new Vector2(0, 0);
        buttonsRT.anchorMax       = new Vector2(1, 0);
        buttonsRT.pivot           = new Vector2(0.5f, 0);
        buttonsRT.anchoredPosition = new Vector2(0, 5);
        buttonsRT.sizeDelta        = new Vector2(0, 35);

        var hlg = buttonsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 5;
        hlg.padding                = new RectOffset(5, 5, 5, 5);
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        // ── Restart Button ────────────────────────────────────────────────
        var restartBtnGO = new GameObject("RestartButton");
        restartBtnGO.transform.SetParent(buttonsGO.transform, false);
        var restartBtnRT = restartBtnGO.AddComponent<RectTransform>();
        restartBtnRT.sizeDelta = new Vector2(0, 0);
        var restartBtnImg = restartBtnGO.AddComponent<Image>();
        restartBtnImg.color = new Color(0.2f, 0.8f, 0.2f);  // Green
        var restartBtn = restartBtnGO.AddComponent<Button>();
        restartBtn.targetGraphic = restartBtnImg;

        var restartTextGO = new GameObject("Text");
        restartTextGO.transform.SetParent(restartBtnGO.transform, false);
        var restartTextRT = restartTextGO.AddComponent<RectTransform>();
        restartTextRT.anchorMin = Vector2.zero;
        restartTextRT.anchorMax = Vector2.one;
        restartTextRT.offsetMin = Vector2.zero;
        restartTextRT.offsetMax = Vector2.zero;
        var restartTextTMP = restartTextGO.AddComponent<TextMeshProUGUI>();
        restartTextTMP.text      = "🔄 Restart";
        restartTextTMP.fontSize  = 14;
        restartTextTMP.color     = Color.white;
        restartTextTMP.alignment = TextAlignmentOptions.Center;

        // ── Menu Button ───────────────────────────────────────────────────
        var menuBtnGO = new GameObject("MenuButton");
        menuBtnGO.transform.SetParent(buttonsGO.transform, false);
        var menuBtnRT = menuBtnGO.AddComponent<RectTransform>();
        menuBtnRT.sizeDelta = new Vector2(0, 0);
        var menuBtnImg = menuBtnGO.AddComponent<Image>();
        menuBtnImg.color = new Color(0.8f, 0.2f, 0.2f);  // Red
        var menuBtn = menuBtnGO.AddComponent<Button>();
        menuBtn.targetGraphic = menuBtnImg;

        var menuTextGO = new GameObject("Text");
        menuTextGO.transform.SetParent(menuBtnGO.transform, false);
        var menuTextRT = menuTextGO.AddComponent<RectTransform>();
        menuTextRT.anchorMin = Vector2.zero;
        menuTextRT.anchorMax = Vector2.one;
        menuTextRT.offsetMin = Vector2.zero;
        menuTextRT.offsetMax = Vector2.zero;
        var menuTextTMP = menuTextGO.AddComponent<TextMeshProUGUI>();
        menuTextTMP.text      = "🏠 Menu";
        menuTextTMP.fontSize  = 14;
        menuTextTMP.color     = Color.white;
        menuTextTMP.alignment = TextAlignmentOptions.Center;

        // Lưu references cho assign vào manager
        _restartButton = restartBtn;
        _menuButton = menuBtn;
    }

    private GameObject CreateRankingItemPrefab()
    {
        // ── Prefab root ───────────────────────────────────────────────────
        var root   = new GameObject("RankingItem");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(0, 30);

        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight  = 30;
        le.flexibleWidth    = 1;

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        // Text (position + name + status)
        var textGO  = new GameObject("RankingText");
        textGO.transform.SetParent(root.transform, false);
        var textRT  = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8, 0);
        textRT.offsetMax = new Vector2(-8, 0);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text      = "#1. Player Name - Status";
        textTMP.fontSize  = 16;
        textTMP.color     = Color.white;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;

        root.SetActive(false); // Prefab ẩn mặc định
        Debug.Log("[RaceRankingsAutoSetup] ✅ RankingItemPrefab created");
        return root;
    }

    private void AssignToDisplay(Transform rankingsContainer, GameObject rankingItemPrefab)
    {
        if (raceRankingsDisplay == null) return;

#if UNITY_EDITOR
        // Dùng SerializedObject để gán đúng trong Editor (undo-able)
        var so = new SerializedObject(raceRankingsDisplay);
        
        // Rankings Container
        var containerProp = so.FindProperty("rankingsContainer");
        if (containerProp != null)
            containerProp.objectReferenceValue = rankingsContainer;

        // Ranking Item Prefab
        var prefabProp = so.FindProperty("rankingItemPrefab");
        if (prefabProp != null)
            prefabProp.objectReferenceValue = rankingItemPrefab;

        // Panel Canvas Group (tìm CanvasGroup trên parent của rankingsContainer)
        if (rankingsContainer != null && rankingsContainer.parent != null)
        {
            var canvasGroup = rankingsContainer.parent.parent?.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                var cgProp = so.FindProperty("panelCanvasGroup");
                if (cgProp != null)
                    cgProp.objectReferenceValue = canvasGroup;
            }
        }

        // Title Text (tìm Title trên parent của rankingsContainer)
        if (rankingsContainer != null)
        {
            var title = rankingsContainer.parent.parent?.Find("Title");
            if (title != null)
            {
                var titleText = title.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    var titleProp = so.FindProperty("titleText");
                    if (titleProp != null)
                        titleProp.objectReferenceValue = titleText;
                }
            }
        }

        // ✅ Restart Button (tìm trong ButtonsContainer)
        if (rankingsContainer != null && _restartButton != null)
        {
            var restartProp = so.FindProperty("restartButton");
            if (restartProp != null)
                restartProp.objectReferenceValue = _restartButton;
        }

        // ✅ Menu Button (tìm trong ButtonsContainer)
        if (rankingsContainer != null && _menuButton != null)
        {
            var menuProp = so.FindProperty("menuButton");
            if (menuProp != null)
                menuProp.objectReferenceValue = _menuButton;
        }

        so.ApplyModifiedProperties();
        Debug.Log("[RaceRankingsAutoSetup] ✅ References assigned via SerializedObject");
#else
        Debug.LogWarning("[RaceRankingsAutoSetup] Runtime assignment không hoạt động với private fields. Chỉ dùng trong Editor.");
#endif
    }
}

// ─────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[CustomEditor(typeof(RaceRankingsAutoSetup))]
public class RaceRankingsAutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        var setup = (RaceRankingsAutoSetup)target;
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Setup Race Rankings UI", GUILayout.Height(40)))
            setup.SetupRaceRankingsUI();
        GUI.backgroundColor = Color.white;
    }
}
#endif
