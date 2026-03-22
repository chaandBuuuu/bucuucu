using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
{
    public string itemName = "Item";       // Name of the item
    public GameObject popupUI;             // Assign a UI Text or Canvas element in Inspector
    private bool isPlayerNearby = false;

    void Start()
    {
        if (popupUI != null)
            popupUI.SetActive(false); // Hide popup at start
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            Pickup();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (popupUI != null)
            {
                popupUI.SetActive(true);
                popupUI.GetComponent<Text>().text = "Press F to pickup " + itemName;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (popupUI != null)
                popupUI.SetActive(false);
        }
    }

    void Pickup()
    {
        Debug.Log(itemName + " picked up!");
        if (popupUI != null)
            popupUI.SetActive(false);

        Destroy(gameObject); // Remove item from scene
    }
}