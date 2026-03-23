using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào từng item trên map
/// Yêu cầu: Collider2D với Is Trigger = true
/// Player cần có tag "Player" và component InventorySystem
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName        = "Item";
    [TextArea]
    public string itemDescription = "";

    [Header("Icon hiện trong Inventory")]
    public Sprite itemSprite; // ← Kéo sprite của item vào đây trong Inspector

    [Header("Popup UI")]
    [SerializeField] private GameObject popupUI;
    [SerializeField] private TMP_Text   popupText;

    private bool            _isPlayerNearby  = false;
    private InventorySystem _nearbyInventory = null;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Tự động lấy sprite từ SpriteRenderer nếu chưa gán
        if (itemSprite == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) itemSprite = sr.sprite;
        }

        if (popupUI != null) popupUI.SetActive(false);
    }

    private void Update()
    {
        if (_isPlayerNearby && Input.GetKeyDown(KeyCode.F))
            TryPickup();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _nearbyInventory = other.GetComponent<InventorySystem>();
        if (_nearbyInventory == null) return;
        _isPlayerNearby = true;
        ShowPopup(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerNearby  = false;
        _nearbyInventory = null;
        ShowPopup(false);
    }

    private void TryPickup()
    {
        if (_nearbyInventory == null) return;

        bool success = _nearbyInventory.AddItem(gameObject);

        if (success)
        {
            Debug.Log($"[ItemPickup] Đã nhặt: {itemName}");
            ShowPopup(false);
        }
        else
        {
            Debug.Log("[ItemPickup] Inventory đầy!");
            if (popupText != null) popupText.text = "Inventory đầy!";
        }
    }

    private void ShowPopup(bool show)
    {
        if (popupUI == null) return;
        popupUI.SetActive(show);
        if (show && popupText != null)
            popupText.text = $"Bấm F để nhặt: {itemName}";
    }
}