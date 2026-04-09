using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bẫy - làm chậm xe khi đi qua trong 3 giây
/// </summary>
public class TrapObject : MonoBehaviour
{
    [Header("Trap Config")]
    [SerializeField] private float slowAmount = 0.6f; // Giảm 60% tốc độ
    [SerializeField] private float slowDuration = 3f;
    
    private HashSet<CarController> _affectedCars = new HashSet<CarController>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var carController = collision.GetComponent<CarController>();
        if (carController == null || _affectedCars.Contains(carController)) return;

        _affectedCars.Add(carController);
        carController.RPC_ApplySlow(slowAmount, slowDuration);
        
        Debug.Log($"[TrapObject] {carController.name} hit trap!");
    }

    private void Start()
    {
        // Trap tự hủy sau 10 giây hoặc khi 3 cars hit
        Destroy(gameObject, 15f);
    }
}
