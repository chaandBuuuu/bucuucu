using TMPro;
using UnityEngine;

/// <summary>
/// ✅ Display một ranking item (position, player name, finish time)
/// </summary>
public class RankingItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text timeText;

    public void Initialize(int position, string playerName, float finishTime)
    {
        if (positionText != null)
        {
            positionText.text = $"#{position}";
            // Color: Gold cho hạng 1, Silver cho hạng 2, Bronze cho hạng 3
            if (position == 1)
                positionText.color = new Color(1f, 0.84f, 0f);        // Gold
            else if (position == 2)
                positionText.color = new Color(0.75f, 0.75f, 0.75f);  // Silver
            else if (position == 3)
                positionText.color = new Color(0.8f, 0.5f, 0.2f);     // Bronze
            else
                positionText.color = Color.white;
        }

        if (playerNameText != null)
            playerNameText.text = playerName;

        if (timeText != null)
            timeText.text = $"{finishTime:F2}s";

        Debug.Log($"[RankingItemUI] Initialized: #{position} {playerName} - {finishTime:F2}s");
    }
}
