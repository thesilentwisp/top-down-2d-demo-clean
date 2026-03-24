using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private bool destroyOnDeath;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0f;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
    }

    public void ResetHealth()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

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
