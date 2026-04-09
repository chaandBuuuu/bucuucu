using UnityEngine;

/// <summary>
/// Pickup powerup trên đường
/// </summary>
public class PowerupPickup : MonoBehaviour
{
    [SerializeField] private PowerupType powerupType;
    [SerializeField] private float respawnTime = 10f;
    private bool _isAvailable = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isAvailable) return;

        var carController = collision.GetComponent<CarController>();
        if (carController == null) return;

        carController.PickupPowerup(powerupType);
        _isAvailable = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Respawn
        Invoke(nameof(Respawn), respawnTime);
    }

    private void Respawn()
    {
        _isAvailable = true;
        GetComponent<SpriteRenderer>().enabled = true;
    }
}
