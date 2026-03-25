using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerFireballAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Projectile fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int fireballDamage = 35;
    [SerializeField] private float channelTimeSeconds = 2f;
    [SerializeField] private float fireCooldown = 1.5f;
    [SerializeField] private float moveSpeedMultiplierWhileChanneling = 0.5f;
    [SerializeField] private InputActionProperty fireAction;

    private PlayerController playerController;
    private InputAction defaultFireAction;
    private float nextFireTime;
    private bool isChanneling;
    private bool hasFiredThisChannel;
    private float channelStartTime;
    private bool slowApplied;
    private float cachedMoveSpeed;

    private InputAction ActiveFireAction => fireAction.action ?? defaultFireAction;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (fireAction.action == null)
        {
            defaultFireAction = new InputAction(name: "Fire");
            defaultFireAction.AddBinding("<Keyboard>/space");
            defaultFireAction.AddBinding("<Gamepad>/rightShoulder");
        }

        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void OnEnable()
    {
        ActiveFireAction?.Enable();
        EndChannel();
    }

    private void OnDisable()
    {
        ActiveFireAction?.Disable();
        EndChannel();
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

        if (ActiveFireAction.WasPressedThisFrame())
        {
            TryStartChannel();
        }

        if (ActiveFireAction.WasReleasedThisFrame())
        {
            CancelChannel();
            return;
        }

        if (!isChanneling || hasFiredThisChannel)
        {
            return;
        }

        float elapsed = Time.time - channelStartTime;
        if (elapsed >= Mathf.Max(0.05f, channelTimeSeconds))
        {
            FireChargedProjectile();
            hasFiredThisChannel = true;
            EndChannel();
            nextFireTime = Time.time + Mathf.Max(0.1f, fireCooldown);
        }
    }

    private void TryStartChannel()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        channelStartTime = Time.time;
        isChanneling = true;
        hasFiredThisChannel = false;
        ApplyChannelMoveSlow();
    }

    private void CancelChannel()
    {
        EndChannel();
    }

    private void FireChargedProjectile()
    {
        Vector2 direction = GetMouseAimDirection();

        Projectile projectile = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        projectile.Initialize(direction, Mathf.Max(1, fireballDamage), gameObject);
        projectile.ConfigureProjectileInteraction(canDestroyOtherProjectiles: true, canPassThroughProjectiles: true);
    }


    private void EndChannel()
    {
        isChanneling = false;
        hasFiredThisChannel = false;
        RemoveChannelMoveSlow();
    }

    private void ApplyChannelMoveSlow()
    {
        if (playerController == null || slowApplied)
        {
            return;
        }

        cachedMoveSpeed = playerController.MoveSpeed;
        float multiplier = Mathf.Clamp(moveSpeedMultiplierWhileChanneling, 0.05f, 1f);
        playerController.MoveSpeed = cachedMoveSpeed * multiplier;
        slowApplied = true;
    }

    private void RemoveChannelMoveSlow()
    {
        if (playerController == null || !slowApplied)
        {
            return;
        }

        playerController.MoveSpeed = cachedMoveSpeed;
        slowApplied = false;
    }

    private Vector2 GetMouseAimDirection()
    {
        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
        {
            return playerController != null ? playerController.AimDirection : Vector2.right;
        }

        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector3 mouseWorld = cameraToUse.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(cameraToUse.transform.position.z)));

        Vector2 direction = mouseWorld - firePoint.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return playerController != null ? playerController.AimDirection : Vector2.right;
        }

        return direction.normalized;
    }
}
