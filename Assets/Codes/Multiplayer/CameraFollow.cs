using UnityEngine;

/// <summary>
/// Script để camera theo dõi nhân vật
/// Được gắn vào Camera object và follow player owner
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private bool constrainToLevelBounds = true;

    [Header("Level Bounds")]
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

    private Transform target;
    private Vector3 targetPosition;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Tính vị trí mục tiêu
        targetPosition = target.position + offset;

        // Giới hạn camera trong bounds nếu cần
        if (constrainToLevelBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Smooth move camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    /// <summary>Đặt target cho camera</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>Đặt offset camera</summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>Đặt bounds camera</summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }
}
