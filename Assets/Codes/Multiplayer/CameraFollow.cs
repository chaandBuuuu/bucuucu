using UnityEngine;

/// <summary>
/// ✅ FIXED: Camera cố định top-down view để dễ quan sát toàn bộ track
/// - Không follow player nữa
/// - Hiển thị toàn bộ arena đua từ trên xuống
/// - Position được set thủ công trong scene (ở giữa track)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 fixedPosition = new Vector3(50, 30, -10);  // Điều chỉnh tùy theo track size
    [SerializeField] private float orthoSize = 25f;  // Zoom level để nhìn toàn bộ track

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
            _camera = gameObject.AddComponent<Camera>();

        // Cài đặt camera
        _camera.orthographic = true;
        _camera.orthographicSize = orthoSize;
        transform.position = fixedPosition;
        
        Debug.Log($"[CameraFollow] ✅ Fixed camera mode - Top-down overview at {fixedPosition}");
    }

    public void SetTarget(Transform target)
    {
        // ✅ DISABLED: Không còn follow player nữa
        Debug.Log($"[CameraFollow] ⚠️ SetTarget() - Camera đang ở chế độ fixed, không follow player");
    }

    /// <summary>
    /// Có thể gọi từ scene setup để điều chỉnh vị trí camera
    /// </summary>
    public void SetFixedPosition(Vector3 newPosition)
    {
        fixedPosition = newPosition;
        transform.position = fixedPosition;
        Debug.Log($"[CameraFollow] Camera vị trí mới: {fixedPosition}");
    }

    /// <summary>
    /// Có thể gọi từ scene setup để điều chỉnh zoom level
    /// </summary>
    public void SetZoom(float newOrthoSize)
    {
        orthoSize = newOrthoSize;
        if (_camera != null)
            _camera.orthographicSize = orthoSize;
        Debug.Log($"[CameraFollow] Camera zoom: {orthoSize}");
    }
}