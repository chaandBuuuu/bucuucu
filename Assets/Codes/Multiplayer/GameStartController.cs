using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

/// <summary>
/// Host bấm Start → load scene ngay, không điều kiện gì thêm
/// </summary>
public class GameStartController : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startRaceButton;

    [Networked] private bool RaceStarting { get; set; }

    public override void Spawned()
    {
        if (startRaceButton != null)
        {
            startRaceButton.onClick.AddListener(OnStartRaceClicked);
            startRaceButton.gameObject.SetActive(HasStateAuthority); // Chỉ host thấy nút
        }

        if (statusText != null)
            statusText.text = "Chờ Host bắt đầu...";

        Debug.Log("[GameStartController] Spawned!");
    }

    private void OnStartRaceClicked()
    {
        if (!HasStateAuthority || RaceStarting) return;

        Debug.Log("[GameStartController] Host bắt đầu → Load Racing Scene");
        RaceStarting = true;
        Runner.LoadScene(SceneRef.FromIndex(2));
    }
}