using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot UI")]
    [SerializeField] private Image[]    itemIcons       = new Image[6];
    [SerializeField] private TMP_Text[] slotNumbers     = new TMP_Text[6];
    [SerializeField] private Image[]    selectionFrames = new Image[6];

    private InventorySystem _inventory;

    private void Start()
    {
        StartCoroutine(FindInventoryWhenReady());
    }

    private System.Collections.IEnumerator FindInventoryWhenReady()
    {
        // Chờ LocalInstance được set (khi player spawn xong)
        while (InventorySystem.LocalInstance == null)
            yield return new WaitForSeconds(0.2f);

        _inventory = InventorySystem.LocalInstance;
        Debug.Log("[InventoryUI] Tìm thấy InventorySystem!");

        _inventory.OnInventoryChanged += RefreshUI;
        _inventory.OnSlotSelected     += OnSlotSelected;

        InitSlotNumbers();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_inventory == null) return;
        _inventory.OnInventoryChanged -= RefreshUI;
        _inventory.OnSlotSelected     -= OnSlotSelected;
    }

    private void InitSlotNumbers()
    {
        for (int i = 0; i < slotNumbers.Length; i++)
            if (slotNumbers[i] != null)
                slotNumbers[i].text = (i + 1).ToString();
    }

    private void RefreshUI()
    {
        if (_inventory == null) return;

        for (int i = 0; i < _inventory.MaxSlots; i++)
        {
            GameObject item = _inventory.GetItem(i);

            if (itemIcons[i] == null) continue;

            if (item != null)
            {
                ItemPickup pickup = item.GetComponent<ItemPickup>();
                Sprite icon = pickup?.itemSprite;

                Debug.Log($"[InventoryUI] Slot {i}: '{item.name}', sprite={(icon != null ? icon.name : "null")}");

                itemIcons[i].sprite  = icon;
                itemIcons[i].color   = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                itemIcons[i].enabled = true;
            }
            else
            {
                itemIcons[i].sprite  = null;
                itemIcons[i].color   = new Color(0, 0, 0, 0);
                itemIcons[i].enabled = false;
            }

            if (selectionFrames[i] != null)
                selectionFrames[i].gameObject.SetActive(false);
        }

        OnSlotSelected(_inventory.SelectedSlot);
    }

    private void OnSlotSelected(int index)
    {
        for (int i = 0; i < selectionFrames.Length; i++)
        {
            if (selectionFrames[i] == null) continue;
            selectionFrames[i].gameObject.SetActive(i == index);
        }
    }
}