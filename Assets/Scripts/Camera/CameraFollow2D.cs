using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 currentVelocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        if (target == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (smoothTime <= 0f)
        {
            transform.position = desiredPosition;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }
}
