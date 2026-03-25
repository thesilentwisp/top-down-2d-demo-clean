using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionProperty moveAction;

    private Rigidbody2D rb;
    private Health health;
    private InputAction defaultMoveAction;
    private Vector2 movementInput;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Health Health => health;

    private InputAction ActiveMoveAction => moveAction.action ?? defaultMoveAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

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
    }

    private void OnEnable()
    {
        ActiveMoveAction?.Enable();
    }

    private void OnDisable()
    {
        ActiveMoveAction?.Disable();
    }

    private void OnDestroy()
    {
        defaultMoveAction?.Dispose();
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
    }

    private void FixedUpdate()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = movementInput * moveSpeed;
#else
        rb.velocity = movementInput * moveSpeed;
#endif
    }
}