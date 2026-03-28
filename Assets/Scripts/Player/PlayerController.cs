using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Dodge")]
    [SerializeField] private InputActionProperty dodgeAction;
    [SerializeField] private float dodgeDistance = 2.25f;
    [SerializeField] private float dodgeDuration = 0.15f;
    [SerializeField] private float dodgeCooldown = 0.6f;
    [SerializeField] private LayerMask dodgeObstacleMask = ~0;
    [SerializeField] private string dodgeAnimationTrigger = "Dodge";

    [Header("Dash Attack")]
    [SerializeField] private InputActionProperty dashAction;
    [SerializeField] private float dashWindupDuration = 1.2f;
    [SerializeField] private float dashDistance = 3.75f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1.5f;
    [SerializeField] private int dashDamage = 20;
    [SerializeField] private LayerMask dashDamageMask = ~0;
    [SerializeField] private LayerMask dashObstacleMask = ~0;
    [SerializeField] private LayerMask dashPassthroughMask;
    [SerializeField] private string dashAnimationTrigger = "Dash";

    private enum DashPhase
    {
        None,
        Windup,
        Dashing
    }

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Health health;

    private InputAction defaultMoveAction;
    private InputAction defaultDodgeAction;
    private InputAction defaultDashAction;

    private Vector2 movementInput;
    private Vector2 lastNonZeroMove = Vector2.right;

    private bool isDodging;
    private float dodgeEndTime;
    private float nextDodgeTime;
    private Vector2 dodgeVelocity;

    private DashPhase dashPhase;
    private float dashWindupEndTime;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashVelocity;
    private Vector2 queuedDashDirection;
    private bool hurdlePhasingEnabled;
    private bool inputEnabled = true;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private readonly Collider2D[] dashDamageHits = new Collider2D[16];
    private readonly HashSet<GameObject> damagedDuringDash = new();

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Health Health => health;
    public Vector2 AimDirection => lastNonZeroMove;
    public bool IsMoving => movementInput.sqrMagnitude > 0.001f;
    public bool IsDodging => isDodging;
    public bool IsDashing => dashPhase != DashPhase.None;
    public bool InputEnabled => inputEnabled;

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (enabled)
        {
            return;
        }

        movementInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (isDodging)
        {
            StopDodge();
        }

        if (IsDashing)
        { 
            StopDash();
        }
    }

    private InputAction ActiveMoveAction => moveAction.action ?? defaultMoveAction;
    private InputAction ActiveDodgeAction => dodgeAction.action ?? defaultDodgeAction;
    private InputAction ActiveDashAction => dashAction.action ?? defaultDashAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        health = GetComponent<Health>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (rb.interpolation == RigidbodyInterpolation2D.None)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (moveAction.action == null)
        {
            defaultMoveAction = new InputAction(name: "Move");
            defaultMoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            defaultMoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            defaultMoveAction.AddBinding("<Gamepad>/leftStick");
        }

        if (dodgeAction.action == null)
        {
            defaultDodgeAction = new InputAction(name: "Dodge");
            defaultDodgeAction.AddBinding("<Mouse>/rightButton");
            defaultDodgeAction.AddBinding("<Gamepad>/buttonEast");
        }

        if (dashAction.action == null)
        {
            defaultDashAction = new InputAction(name: "Dash Attack");
            defaultDashAction.AddBinding("<Keyboard>/rightShift");
            defaultDashAction.AddBinding("<Keyboard>/leftShift");
            defaultDashAction.AddBinding("<Gamepad>/leftShoulder");
        }

    }

    private void OnEnable()
    {
        ActiveMoveAction?.Enable();
        ActiveDodgeAction?.Enable();
        ActiveDashAction?.Enable();
    }

    private void OnDisable()
    {
        ActiveMoveAction?.Disable();
        ActiveDodgeAction?.Disable();
        ActiveDashAction?.Disable();

        EnableHurdlePhasing(false);
        health?.SetInvulnerable(false);
    }

    private void OnDestroy()
    {
        defaultMoveAction?.Dispose();
        defaultDodgeAction?.Dispose();
        defaultDashAction?.Dispose();
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateAbilityState();

        if (!inputEnabled)
        {
            return;
        }

        if (CanStartAbility() && ActiveDashAction != null && ActiveDashAction.WasPressedThisFrame())
        {
            TryStartDashWindup();
        }

        if (CanStartAbility() && ActiveDodgeAction != null && ActiveDodgeAction.WasPressedThisFrame())
        {
            TryStartDodge();
        }
    }

    private void FixedUpdate()
    {
        if (!inputEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (dashPhase == DashPhase.Windup)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (dashPhase == DashPhase.Dashing)
        {
            rb.linearVelocity = dashVelocity;
            DamageDashContacts();
            return;
        }

        if (isDodging)
        {
            rb.linearVelocity = dodgeVelocity;
            return;
        }

        rb.linearVelocity = movementInput * moveSpeed;
    }

    private void ReadMovementInput()
    {
        if(!inputEnabled)
        {
            movementInput = Vector2.zero;
            return;
        }

        if (ActiveMoveAction == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        if (dashPhase == DashPhase.Windup)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = Vector2.ClampMagnitude(ActiveMoveAction.ReadValue<Vector2>(), 1f);

        if (movementInput.sqrMagnitude > 0.001f)
        {
            lastNonZeroMove = movementInput.normalized;
            UpdateFacingVisual();
        }
    }

    private void UpdateAbilityState()
    {
        if (isDodging && Time.time >= dodgeEndTime)
        {
            StopDodge();
        }

        if (dashPhase == DashPhase.Windup && Time.time >= dashWindupEndTime)
        {
            StartDash();
        }

        if (dashPhase == DashPhase.Dashing && Time.time >= dashEndTime)
        {
            StopDash();
        }
    }

    private bool CanStartAbility()
    {
        return !isDodging && dashPhase == DashPhase.None;
    }

    private void TryStartDodge()
    {
        if (Time.time < nextDodgeTime)
        {
            return;
        }

        Vector2 dodgeDirection = movementInput.sqrMagnitude > 0.001f ? movementInput.normalized : lastNonZeroMove;
        if (dodgeDirection.sqrMagnitude <= 0.001f)
        {
            dodgeDirection = Vector2.right;
        }

        float allowedDistance = GetAllowedMoveDistance(dodgeDirection, Mathf.Max(0.05f, dodgeDistance), dodgeObstacleMask);
        if (allowedDistance <= 0.02f)
        {
            return;
        }

        float duration = Mathf.Max(0.03f, dodgeDuration);
        dodgeVelocity = dodgeDirection * (allowedDistance / duration);
        isDodging = true;
        dodgeEndTime = Time.time + duration;
        nextDodgeTime = Time.time + Mathf.Max(duration, dodgeCooldown);
        movementInput = Vector2.zero;

        if (animator != null && !string.IsNullOrWhiteSpace(dodgeAnimationTrigger))
        {
            animator.SetTrigger(dodgeAnimationTrigger);
        }
    }

    private void StopDodge()
    {
        isDodging = false;
        dodgeVelocity = Vector2.zero;
    }

    private void TryStartDashWindup()
    {
        if (Time.time < nextDashTime)
        {
            return;
        }

        Vector2 dashDirection = movementInput.sqrMagnitude > 0.001f ? movementInput.normalized : lastNonZeroMove;
        if (dashDirection.sqrMagnitude <= 0.001f)
        {
            dashDirection = Vector2.right;
        }

        float allowedDistance = GetAllowedDashDistance(dashDirection, Mathf.Max(0.05f, dashDistance));
        if (allowedDistance <= 0.02f)
        {
            return;
        }

        queuedDashDirection = dashDirection;
        dashPhase = DashPhase.Windup;
        dashWindupEndTime = Time.time + Mathf.Max(0.05f, dashWindupDuration);
        movementInput = Vector2.zero;
    }

    private void StartDash()
    {
        float allowedDistance = GetAllowedDashDistance(queuedDashDirection, Mathf.Max(0.05f, dashDistance));
        float duration = Mathf.Max(0.03f, dashDuration);

        if (allowedDistance <= 0.02f)
        {
            dashPhase = DashPhase.None;
            nextDashTime = Time.time + Mathf.Max(0.1f, dashCooldown);
            return;
        }

        dashVelocity = queuedDashDirection * (allowedDistance / duration);
        dashPhase = DashPhase.Dashing;
        dashEndTime = Time.time + duration;
        nextDashTime = Time.time + Mathf.Max(duration, dashCooldown);
        damagedDuringDash.Clear();

        health?.SetInvulnerable(true);
        EnableHurdlePhasing(true);

        if (animator != null && !string.IsNullOrWhiteSpace(dashAnimationTrigger))
        {
            animator.SetTrigger(dashAnimationTrigger);
        }
    }

    private void StopDash()
    {
        dashPhase = DashPhase.None;
        dashVelocity = Vector2.zero;
        damagedDuringDash.Clear();
        health?.SetInvulnerable(false);
        EnableHurdlePhasing(false);
    }

    private void DamageDashContacts()
    {
        if (dashDamage <= 0 || playerCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = dashDamageMask,
            useTriggers = true
        };

        int hitCount = playerCollider.Overlap(filter, dashDamageHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = dashDamageHits[i];
            if (hit == null)
            {
                continue;
            }

            GameObject targetObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.transform.root.gameObject;
            if (targetObject == gameObject)
            {
                continue;
            }

            if (!damagedDuringDash.Add(targetObject))
            {
                continue;
            }

            IDamageable damageable = targetObject.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(Mathf.Max(1, dashDamage));
        }
    }

    private void EnableHurdlePhasing(bool enabled)
    {
        if (hurdlePhasingEnabled == enabled)
        {
            return;
        }

        int playerLayer = gameObject.layer;
        int maskValue = dashPassthroughMask.value;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((maskValue & (1 << layer)) == 0)
            {
                continue;
            }

            Physics2D.IgnoreLayerCollision(playerLayer, layer, enabled);
        }

        hurdlePhasingEnabled = enabled;
    }


    private float GetAllowedDashDistance(Vector2 direction, float intendedDistance)
    {
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = dashObstacleMask,
            useTriggers = false
        };

        int hitCount = rb.Cast(direction, filter, castHits, intendedDistance);
        if (hitCount <= 0)
        {
            return intendedDistance;
        }

        float nearestBlockingHit = intendedDistance;
        bool foundBlockingHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (!IsDashBlockingHit(hit))
            {
                continue;
            }

            if (!foundBlockingHit || hit.distance < nearestBlockingHit)
            {
                nearestBlockingHit = hit.distance;
                foundBlockingHit = true;
            }
        }

        if (!foundBlockingHit)
        {
            return intendedDistance;
        }

        const float skin = 0.05f;
        return Mathf.Max(0f, nearestBlockingHit - skin);
    }

    private bool IsDashBlockingHit(RaycastHit2D hit)
    {
        Collider2D hitCollider = hit.collider;
        if (hitCollider == null)
        {
            return false;
        }

        if ((dashPassthroughMask.value & (1 << hitCollider.gameObject.layer)) != 0)
        {
            return false;
        }

        if ((dashDamageMask.value & (1 << hitCollider.gameObject.layer)) != 0)
        {
            return false;
        }

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null && hitCollider.transform.root != transform.root)
        {
            return false;
        }

        return true;
    }

    private float GetAllowedMoveDistance(Vector2 direction, float intendedDistance, LayerMask obstacleMask)
    {
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };

        int hitCount = rb.Cast(direction, filter, castHits, intendedDistance);
        if (hitCount <= 0)
        {
            return intendedDistance;
        }

        float nearest = intendedDistance;
        for (int i = 0; i < hitCount; i++)
        {
            float hitDistance = castHits[i].distance;
            if (hitDistance < nearest)
            {
                nearest = hitDistance;
            }
        }

        const float skin = 0.05f;
        return Mathf.Max(0f, nearest - skin);
    }

    private void UpdateFacingVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (Mathf.Abs(lastNonZeroMove.x) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = lastNonZeroMove.x < 0f;
    }
}