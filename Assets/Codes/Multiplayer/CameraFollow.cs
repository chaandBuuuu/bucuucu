using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset    = new Vector3(0, 0, -10);

    private Transform _target;

    public void SetTarget(Transform target)
    {
        _target = target;
        Debug.Log($"[CameraFollow] Theo dõi: {target.name}");
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desired  = _target.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        transform.position = smoothed;
    }
}