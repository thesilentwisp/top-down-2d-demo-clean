using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private bool destroyOnDeath;

    private int invulnerabilityLockCount;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvulnerable => invulnerabilityLockCount > 0;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        invulnerabilityLockCount = 0;
    }

    public void ResetHealth()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        invulnerabilityLockCount = 0;
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        if (isInvulnerable)
        {
            invulnerabilityLockCount++;
            return;
        }

        invulnerabilityLockCount = Mathf.Max(0, invulnerabilityLockCount - 1);
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || IsInvulnerable || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (!IsAlive)
        {
            onDeath?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}