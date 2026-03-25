using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]

public class PlayerFireballAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Projectile fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireballDamage = 20f;
    [SerializeField] private float fireCooldown = 0.3f;
    [SerializeField] private InputActionProperty fireAction;

    private PlayerController playerController;
    private InputAction defaultFireAction;
    private float nextFireTime;

    private InputAction ActiveFireAction => fireAction.action ?? defaultFireAction;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (fireAction.action == null)
        {
            defaultFireAction = new InputAction(name: "Fire");
            defaultFireAction.AddBinding("<Mouse>/leftButton");
            defaultFireAction.AddBinding("Gamepad>/rightShoulder");
        }

        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void OnEnable()
    {
        ActiveFireAction?.Enable();
    }

    private void OnDisable()
    {
        ActiveFireAction?.Disable();
    }

    private void OnDestroy()
    {
        defaultFireAction?.Dispose();
    }

    private void Update()
    {
        if (fireballPrefab == null || ActiveFireAction == null)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (!ActiveFireAction.WasPressedThisFrame())
        {
            return;
        }

        FireProjectile();
    }

    private void FireProjectile()
    {
        Vector2 direction = playerController != null ? playerController.AimDirection : Vector2.right;

        Projectile projectile = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        projectile.Initialize(direction, fireballDamage, gameObject);
        nextFireTime = Time.time + Mathf.Max(0.05f, fireCooldown);
    }
}
