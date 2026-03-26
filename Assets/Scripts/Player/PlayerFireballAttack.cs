using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerFireballAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Projectile fireballPrefab;
    [SerializeField] private int fireballDamage = 35;
    [SerializeField] private float channelTimeSeconds = 2f;
    [SerializeField] private float fireCooldown = 1.5f;
    [SerializeField] private float fireballSpawnDistance = 0.75f;
    [SerializeField] private float moveSpeedMultiplierWhileChanneling = 0.5f;
    [SerializeField] private InputActionProperty fireAction;

    private PlayerController playerController;
    private InputAction defaultFireAction;
    private float nextFireTime;
    private bool isChanneling;
    private bool isChargeReady;
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
            ReleaseChannel();
            return;
        }

        if (!isChanneling)
        {
            return;
        }

        float elapsed = Time.time - channelStartTime;
        isChargeReady = elapsed >= Mathf.Max(0.05f, channelTimeSeconds);
    }

    private void TryStartChannel()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        channelStartTime = Time.time;
        isChanneling = true;
        isChargeReady = false;
        ApplyChannelMoveSlow();
    }

    private void ReleaseChannel()
    {
        if (isChanneling && isChargeReady && Time.time >= nextFireTime)
        {
            FireChargedProjectile();
            nextFireTime = Time.time + Mathf.Max(0.1f, fireCooldown);
        }

        EndChannel();
    }

    private void FireChargedProjectile()
    {
        Vector2 direction = GetMouseAimDirection();
        Vector2 spawnPosition = (Vector2)transform.position + direction * Mathf.Max(0.05f, fireballSpawnDistance);

        Projectile projectile = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        projectile.Initialize(direction, Mathf.Max(1, fireballDamage), gameObject);
        projectile.ConfigureProjectileInteraction(canDestroyOtherProjectiles: true, canPassThroughProjectiles: true);
    }


    private void EndChannel()
    {
        isChanneling = false;
        isChargeReady = false;
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

        Vector2 direction = mouseWorld - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return playerController != null ? playerController.AimDirection : Vector2.right;
        }

        return direction.normalized;
    }
}
