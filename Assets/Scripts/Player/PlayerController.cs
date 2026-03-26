using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
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

    private Rigidbody2D rb;
    private Health health;
    private InputAction defaultMoveAction;
    private InputAction defaultDodgeAction;
    private Vector2 movementInput;
    private Vector2 lastNonZeroMove = Vector2.right;
    private bool isDodging;
    private float dodgeEndTime;
    private float nextDodgeTime;
    private Vector2 dodgeVelocity;
    private readonly RaycastHit2D[] dodgeHits = new RaycastHit2D[8];
        

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Health Health => health;
    public Vector2 AimDirection => lastNonZeroMove;
    public bool IsMoving => movementInput.sqrMagnitude > 0.001f;
    public bool IsDodging => isDodging;

    private InputAction ActiveMoveAction => moveAction.action ?? defaultMoveAction;
    private InputAction ActiveDodgeAction => dodgeAction.action ?? defaultDodgeAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            GetComponentInChildren<Animator>();
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
    }

    private void OnEnable()
    {
        ActiveMoveAction?.Enable();
        ActiveDodgeAction?.Enable();
    }

    private void OnDisable()
    {
        ActiveMoveAction?.Disable();
        ActiveDodgeAction?.Disable();
    }

    private void OnDestroy()
    {
        defaultMoveAction?.Dispose();
        defaultDodgeAction?.Dispose();
    }

    private void Update()
    {
        if (ActiveMoveAction == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = ActiveMoveAction.ReadValue<Vector2>();
        movementInput = Vector2.ClampMagnitude(movementInput, 1f);

        if (movementInput.sqrMagnitude > 0.001f)
        {
            lastNonZeroMove = movementInput.normalized;
            UpdateFacingVisual();
        }

        if (isDodging && Time.time >= dodgeEndTime)
        {
            StopDodge();
        }

        if (!isDodging && ActiveDodgeAction != null && ActiveDodgeAction.WasPressedThisFrame())
        {
            TryStartDodge();
        }
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

    private void FixedUpdate()
    {
        if (isDodging)
        {
            rb.linearVelocity = dodgeVelocity;
            return;
        }

        rb.linearVelocity = movementInput * moveSpeed;
    }

    private void TryStartDodge()
    {
        if (Time.time < nextDodgeTime)
        {
            return;
        }

        Vector2 dodgeDirection = movementInput.sqrMagnitude > 0.001f ? movementInput.normalized : lastNonZeroMove;
        if (dodgeDirection.sqrMagnitude <= 0/001f)
        {
            dodgeDirection = Vector2.right;
        }

        float allowedDistance = GetAllowedDodgeDistance(dodgeDirection, Mathf.Max(0.05f, dodgeDistance));
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

    private float GetAllowedDodgeDistance(Vector2 direction, float intendedDistance)
    {
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = dodgeObstacleMask,
            useTriggers = false
        };

        int hitcount = rb.Cast(direction, filter, dodgeHits, intendedDistance);
        if (hitcount <= 0)
        {
            return intendedDistance;
        }

        float nearest = intendedDistance;
        for (int i = 0; i < hitcount; i++)
        {
            float hitDistance = dodgeHits[i].distance;
            if (hitDistance < nearest)
            {
                nearest = hitDistance;
            }
        }

        const float skin = 0.05f;
        return Mathf.Max(0f, nearest - skin);
    }
}