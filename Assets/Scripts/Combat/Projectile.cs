using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetimeSeconds = 5f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Vector2 direction;
    private GameObject owner;
    private bool destroysOtherProjectiles;
    private bool passesThroughProjectiles;

    public void Initialize(Vector2 shootDirection, int projectileDamage, GameObject projectileOwner)
    {
        direction = shootDirection.sqrMagnitude > 0.001f ? shootDirection.normalized : Vector2.right;
        damage = Mathf.Max(0, projectileDamage);
        owner = projectileOwner;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void ConfigureProjectileInteraction(bool canDestroyOtherProjectiles, bool canPassThroughProjectiles)
    {
        destroysOtherProjectiles = canDestroyOtherProjectiles;
        passesThroughProjectiles = canPassThroughProjectiles;
    }

    private void Awake()
    {
        Collider2D projectileCollider = GetComponent<Collider2D>();
        projectileCollider.isTrigger = true;
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

        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        Projectile otherProjectile = other.GetComponentInParent<Projectile>();
        if (otherProjectile != null)
        {
            if (destroysOtherProjectiles)
            {
                Destroy(otherProjectile.gameObject);
            }

            if (passesThroughProjectiles || destroysOtherProjectiles)
            {
                return;
            }
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
