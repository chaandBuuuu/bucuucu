using UnityEngine;
using Fusion;

/// <summary>
/// Quản lý logic inventory
/// Gắn vào prefab Multiplayer (player)
/// </summary>
public class InventorySystem : NetworkBehaviour
{
    // ✅ Static Instance để InventoryUI tìm thấy dù player spawn muộn
    public static InventorySystem LocalInstance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxSlots = 6;

    private GameObject[] _slots;
    private int _selectedSlot = -1;

    public int MaxSlots     => maxSlots;
    public int SelectedSlot => _selectedSlot;

    public event System.Action      OnInventoryChanged;
    public event System.Action<int> OnSlotSelected;

    private void Awake()
    {
        _slots = new GameObject[maxSlots];
    }

    public override void Spawned()
    {
        // Chỉ set Instance cho player của chính mình
        if (HasInputAuthority)
        {
            LocalInstance = this;
            Debug.Log("[InventorySystem] Local instance sẵn sàng!");
        }
    }

    private void OnDestroy()
    {
        if (LocalInstance == this) LocalInstance = null;
    }

    private void Update()
    {
        if (!HasInputAuthority) return;
        HandleSlotInput();
    }

    private void HandleSlotInput()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                break;
            }
        }
    }

    public bool AddItem(GameObject item)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = item;
                item.transform.SetParent(transform);
                item.SetActive(false);

                Debug.Log($"[Inventory] Nhặt '{item.name}' vào ô {i + 1}");

                if (_selectedSlot == -1) SelectSlot(i);

                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        Debug.Log("[Inventory] Inventory đầy!");
        return false;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= maxSlots) return;
        _selectedSlot = index;
        Debug.Log($"[Inventory] Chọn ô {index + 1}: {(_slots[index] != null ? _slots[index].name : "trống")}");
        OnSlotSelected?.Invoke(_selectedSlot);
    }

    public GameObject GetItem(int index)
    {
        if (index < 0 || index >= maxSlots) return null;
        return _slots[index];
    }

    public GameObject GetSelectedItem() =>
        _selectedSlot >= 0 ? _slots[_selectedSlot] : null;

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= maxSlots) return;
        if (_slots[index] != null)
        {
            Destroy(_slots[index]);
            _slots[index] = null;
            OnInventoryChanged?.Invoke();
        }
    }
}