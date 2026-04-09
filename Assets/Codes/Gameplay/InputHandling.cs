using UnityEngine;
using Fusion;

/// <summary>
/// Extension methods for NetworkCharacterController
/// </summary>
public static class CharacterControllerExtensions
{
    /// <summary>
    /// Knockback nhân vật theo hướng
    /// </summary>
    public static void RPC_Knockback(this NetworkCharacterController controller, Vector2 direction, float force)
    {
        var rb = controller.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * force;
        }
    }

    /// <summary>
    /// Kéo nhân vật về một điểm
    /// </summary>
    public static void RPC_PulledByVine(this NetworkCharacterController controller, Vector2 targetPosition, float force)
    {
        var rb = controller.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 direction = (targetPosition - (Vector2)controller.transform.position).normalized;
            rb.linearVelocity = direction * force;
        }
    }
}
