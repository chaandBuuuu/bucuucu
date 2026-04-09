using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// FIX:
///   - Chuyển thành NetworkBehaviour — được spawn qua runner.Spawn() từ PowerupInventory
///     nên tồn tại trên tất cả client. Trước đây Instantiate() chỉ tạo local.
///   - Chỉ server xử lý OnTriggerEnter2D để tránh apply slow 2 lần
///   - Dùng RacingConstants thay vì hardcode
///   - Server tự despawn sau TRAP_LIFETIME
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrapObject : NetworkBehaviour
{
    [Header("Trap Config")]
    [SerializeField] private float slowAmount  = RacingConstants.TRAP_SLOW_AMOUNT;
    [SerializeField] private float slowDuration = RacingConstants.TRAP_SLOW_DURATION;

    // HashSet lưu NetworkId để tránh affect cùng xe nhiều lần
    private HashSet<NetworkId> _affectedCars = new HashSet<NetworkId>();

    public override void Spawned()
    {
        // Chỉ server quản lý lifetime và despawn
        if (Runner.IsServer)
            StartCoroutine(DespawnCoroutine());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // FIX: Chỉ server xử lý → tránh slow được gọi từ nhiều client
        if (!Runner.IsServer) return;

        var car = collision.GetComponent<CarController>();
        if (car == null) return;
        if (car.IsFinished) return;

        NetworkId carId = car.Object.Id;
        if (_affectedCars.Contains(carId)) return;

        _affectedCars.Add(carId);
        car.RPC_ApplySlow(slowAmount, slowDuration);
        Debug.Log($"[TrapObject] {car.name} dính bẫy!");
    }

    private System.Collections.IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(RacingConstants.TRAP_LIFETIME);
        if (Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }
}