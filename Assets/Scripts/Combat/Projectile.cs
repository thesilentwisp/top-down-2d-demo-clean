using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetimeSeconds = 5f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Vector2 direction;
    private GameObject owner;

    public void Initialize(Vector2 shootDirection,float projectileDamage, GameObject projectileOwner)
    {
        direction = shootDirection.sqrMagnitude > 0.001f ? shootDirection.normalized : Vector2.right;
        damage = Mathf.Max(0f, projectileDamage);
        owner = projectileOwner;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Awake()
    {
        Collider2D projectCollider = GetComponent<Collider2D>();
        projectCollider.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, Mathf.Max(0.1f, lifetimeSeconds));
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner)
        {
            return;
        }

        if ((hitMask.value & (1 << other.gameObject.layer)) ==0)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

        Destroy(gameObject);
    }
}
