using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Collider2D))]
public class BasicEnemyShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRadius = 6f;

    [Header("Attack")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private int bulletsPerBurst = 3;
    [FormerlySerializedAs("shootIntervalSeconds")]
    [SerializeField] private float timeBetweenShots = 0.5f;
    [SerializeField] private float postTeleportShootDelay = 0.75f;

    [Header("Teleport")]
    [FormerlySerializedAs("moveDistance")]
    [SerializeField] private float teleportDistance = 2.5f;
    [SerializeField] private int teleportDirectionChecks = 12;
    [FormerlySerializedAs("wallMask")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Patrol")]
    [SerializeField] private float patrolHalfWidth = 2f;
    [SerializeField] private float patrolSpeed = 1.5f;

    private Collider2D enemyCollider;
    private Vector3 patrolCenter;
    private int patrolDirection = 1;
    private float nextShotTime;
    private int burstShotsFired;

    private void Awake()
    {
        enemyCollider = GetComponent<Collider2D>();
        patrolCenter = transform.position;

        if (player == null)
        {
            PlayerController foundPlayer = FindAnyObjectByType<PlayerController>();
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        if (muzzle == null)
        {
            muzzle = transform;
        }
    }

    private void OnEnable()
    {
        nextShotTime = Time.time + 0.2f;
        burstShotsFired = 0;
    }

    private void Update()
    {
        if (player == null)
        {
            Patrol();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInRange = distanceToPlayer <= Mathf.Max(0.1f, detectionRadius);

        if (!playerInRange)
        {
            burstShotsFired = 0;
            Patrol();
            return;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        ShootAtPlayer();
        burstShotsFired++;

        if (burstShotsFired >= Mathf.Max(1, bulletsPerBurst))
        {
            TeleportWithinRange();
            burstShotsFired = 0;
            nextShotTime = Time.time + Mathf.Max(0f, postTeleportShootDelay);
            return;
        }

        nextShotTime = Time.time + Mathf.Max(0.05f, timeBetweenShots);
    }

    private void Patrol()
    {
        float halfWidth = Mathf.Max(0.1f, patrolHalfWidth);
        float targetX = patrolCenter.x + (patrolDirection * halfWidth);

        Vector3 current = transform.position;
        float moveStep = Mathf.Max(0.01f, patrolSpeed) * Time.deltaTime;
        float newX = Mathf.MoveTowards(current.x, targetX, moveStep);
        Vector3 desiredPosition = new Vector3(newX, current.y, current.z);

        if (TryMoveWithObstacleCheck(current, desiredPosition))
        {
            if (Mathf.Abs(newX - targetX) <= 0.01f)
            {
                patrolDirection *= -1;
            }

            return;
        }

        patrolDirection *= -1;
    }

    private bool TryMoveWithObstacleCheck(Vector3 from, Vector3 to)
    {
        Vector2 move = (Vector2)(to - from);
        float distance = move.magnitude;

        if (distance <= 0.0001f)
        {
            return true;
        }

        float castRadius = 0.2f;
        if (enemyCollider != null)
        {
            castRadius = Mathf.Max(enemyCollider.bounds.extents.x, enemyCollider.bounds.extents.y, 0.2f);
        }

        Vector2 direction = move / distance;
        RaycastHit2D[] pathHits = Physics2D.CircleCastAll(from, castRadius, direction, distance, obstacleMask);
        if (HasBlockingHit(pathHits))
        {
            return false;
        }

        Collider2D[] destinationHits = Physics2D.OverlapCircleAll(to, castRadius, obstacleMask);
        if (HasBlockingHit(destinationHits))
        {
            return false;
        }

        transform.position = to;
        return true;
    }

    private void ShootAtPlayer()
    {
        Vector2 dirToPlayer = player.position - muzzle.position;
        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        projectile.Initialize(dirToPlayer, projectileDamage, gameObject);
    }

    private void TeleportWithinRange()
    {
        Vector3 currentPosition = transform.position;

        if (TryGetTeleportTarget(out Vector3 teleportTarget))
        {
            transform.position = teleportTarget;
            patrolCenter = teleportTarget;
            return;
        }

        transform.position = currentPosition;
    }

    private bool TryGetTeleportTarget(out Vector3 teleportTarget)
    {
        teleportTarget = transform.position;

        int checks = Mathf.Max(4, teleportDirectionChecks);
        float castRadius = 0.2f;
        if (enemyCollider != null)
        {
            castRadius = Mathf.Max(enemyCollider.bounds.extents.x, enemyCollider.bounds.extents.y, 0.2f);
        }

        for (int i = 0; i < checks; i++)
        {
            float angle = (360f / checks) * i;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 candidate = (Vector2)transform.position + direction * Mathf.Max(0.25f, teleportDistance);

            if (Vector2.Distance(candidate, player.position) > detectionRadius)
            {
                continue;
            }

            RaycastHit2D[] pathHits = Physics2D.CircleCastAll(transform.position, castRadius, direction, teleportDistance, obstacleMask);
            if (HasBlockingHit(pathHits))
            {
                continue;
            }

            Collider2D[] destinationHits = Physics2D.OverlapCircleAll(candidate, castRadius, obstacleMask);
            if (HasBlockingHit(destinationHits))
            {
                continue;
            }

            teleportTarget = candidate;
            return true;
        }

        return false;
    }

    private bool HasBlockingHit(RaycastHit2D[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider != null && hitCollider != enemyCollider)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasBlockingHit(Collider2D[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i];
            if (hitCollider != null && hitCollider != enemyCollider)
            {
                return true;
            }
        }

        return false;
    }
}
