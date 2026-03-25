using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Melee")]
    [SerializeField] private int damage = 12;
    [SerializeField] private float cooldownSeconds = 0.35f;
    [SerializeField] private float attackRange = 0.9f;
    [SerializeField] private float attackRadius = 0.65f;
    [SerializeField] private float knockbackImpulse = 6f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private InputActionProperty attackAction;

    private PlayerController playerController;
    private InputAction defaultAttackAction;
    private float nextAttackTime;

    private InputAction ActiveAttackAction => attackAction.action ?? defaultAttackAction;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (attackAction.action == null)
        {
            defaultAttackAction = new InputAction(name: "Melee Attack");
            defaultAttackAction.AddBinding("<Mouse>/leftButton");
            defaultAttackAction.AddBinding("<Gamepad>/buttonSouth");
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }
    }

    private void OnEnable()
    {
        ActiveAttackAction?.Enable();
    }

    private void OnDisable()
    {
        ActiveAttackAction?.Disable();
    }

    private void OnDestroy()
    {
        defaultAttackAction?.Dispose();
    }

    private void Update()
    {
        if (ActiveAttackAction == null)
        {
            return;
        }

        if (!ActiveAttackAction.WasPressedThisFrame())
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        PerformMeleeAttack();
        nextAttackTime = Time.time + Mathf.Max(0.05f, cooldownSeconds);
    }

   private void PerformMeleeAttack()
    {
        Vector2 facing = playerController != null ? playerController.AimDirection : Vector2.right;
        Vector2 attackCenter = (Vector2)attackOrigin.position + facing.normalized * Mathf.Max(0.1f, attackRange);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, Mathf.Max(0.05f, attackRadius), targetMask);
        HashSet<GameObject> processedTargets = new();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            GameObject targetObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.transform.root.gameObject;

            if (!processedTargets.Add(targetObject))
            {
                continue;
            }

            IDamageable damageable = targetObject.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Mathf.Max(1, damage));
            }

            ApplyKnockback(targetObject.transform.position, targetObject.transform, hit.attachedRigidbody);
        }
    }

    private void ApplyKnockback(Vector3 targetPosition, Transform targetTransform, Rigidbody2D targetBody)
    {
        Vector2 pushDirection = (targetPosition - transform.position);
        if(pushDirection.sqrMagnitude <= 0.001f)
        {
            pushDirection = playerController != null ? playerController.AimDirection : Vector2.right;
        }

        pushDirection = pushDirection.normalized;

        if (targetBody != null)
        {
            targetBody.AddForce(pushDirection * Mathf.Max(0f, knockbackImpulse), ForceMode2D.Impulse);
            return;
        }

        if (targetTransform == null)
        {
            return;
        }

        targetTransform.position += (Vector3)(pushDirection * (Mathf.Max(0f, knockbackImpulse) * 0.04f));
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector2 facing = Application.isPlaying && playerController != null ? playerController.AimDirection : Vector2.right;
        Vector2 attackCenter = (Vector2)origin.position + facing.normalized * Mathf.Max(0.1f, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackCenter, Mathf.Max(0.05f, attackRadius));
    }
}

