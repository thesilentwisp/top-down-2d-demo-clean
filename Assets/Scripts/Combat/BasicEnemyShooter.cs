using TMPro;
using UnityEngine;

[RequireComponent (typeof(Health))]
[RequireComponent (typeof(Collider2D))]

public class BasicEnemyShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Attack")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float shootIntervalSeconds = 1.5f;

    [Header("Reposition")]
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float movedurationSeconds = .25f;
    [SerializeField] private float postMoveShootDelay = .15f;
    [SerializeField] private LayerMask wallMask;

    private Collider2D enemyCollider;
    private float nextShootTime;
    private bool isMoving;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private float moveStartTime;

    private void Awake()
    {
        enemyCollider = GetComponent<Collider2D>();

        if (player == null)
        {
            PlayerController foundPlayer = FindAnyObjectByType<PlayerController>();

            if (foundPlayer !=null)
            {
                player = foundPlayer.transform;
            }
        }

        if (muzzle == null)
        {
            muzzle = transform;
        }
    }

    private void Update()
    {
        if (player == null || projectilePrefab == null)
        {
            return;
        }

        if (isMoving)
        {
            UpdateMove();
            return;
        }

        if (Time.time >= nextShootTime)
        {
            ShootAtPlayer();
            StartMoveAway();
        }
    }

    private void ShootAtPlayer()
    {
        Vector2 dirToPlayer = (player.position - muzzle.position);
        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        projectile.Initialize(dirToPlayer, projectileDamage, gameObject);
    }

    private void StartMoveAway()
    {
        Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            Vector2 fallback = Quaternion.Euler(0f, 0f, 35f) * awayFromPlayer;
            if(!TryGetValidMoveTarget(awayFromPlayer, out moveTarget))
            {
                nextShootTime = Time.time + shootIntervalSeconds;
                return;
            }
        }

        moveStart = transform.position;
        moveStartTime = Time.time;
        isMoving = true;
    }

    private bool TryGetValidMoveTarget(Vector2 direction, out Vector3 validTarget)
    {
        Vector2 origin = transform.position;
        Vector2 target = origin + direction.normalized * moveDistance;

        float castRadius = 0.2f;
        if (enemyCollider != null)
        {
            castRadius = Mathf.Max(enemyCollider.bounds.extents.x, enemyCollider.bounds.extents.y, 0.2f);
        }

        RaycastHit2D wallBetween = Physics2D.CircleCast(origin, castRadius, direction, moveDistance, wallMask);
        if(wallBetween.collider !=null)
        {
            validTarget = transform.position;
            return false;
        }

        Collider2D blockedAtDestination = Physics2D.OverlapCircle(target, castRadius, wallMask);
        if (blockedAtDestination !=null)
        {
            validTarget = transform.position;
            return false;
        }

        validTarget = target;
        return true;
    }

    private void UpdateMove()
    {
        float elapsed = Time.time - moveStartTime;
        float duration = Mathf.Max(0.01f, movedurationSeconds);
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(moveStart, moveTarget, t);

        if (t >=1)
        {
            isMoving = false;
            nextShootTime = Time.time + postMoveShootDelay;
        }
    }
}
