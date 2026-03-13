// KalbHealth.cs - Add debug logging
using UnityEngine;

public class KalbHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private bool isInvulnerable = false;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => isInvulnerable;

    public System.Action<int, Vector2> OnDamaged;
    public System.Action OnHealed;
    public System.Action OnDeath;

    private void Start()
    {
        if (enableDebugLogs) Debug.Log($"[Health] Initialized with {currentHealth}/{maxHealth} HP");
    }

    public void TakeDamage(int damage, Vector2 hitSource)
    {
        if (enableDebugLogs) Debug.Log($"[Health] TakeDamage called - Damage: {damage}, Current HP: {currentHealth}, Invulnerable: {isInvulnerable}, IsDead: {IsDead}");

        if (IsDead)
        {
            if (enableDebugLogs) Debug.Log("[Health] Cannot take damage - already dead");
            return;
        }

        if (isInvulnerable)
        {
            if (enableDebugLogs) Debug.Log("[Health] Cannot take damage - invulnerable");
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (enableDebugLogs) Debug.Log($"[Health] Damage applied! New HP: {currentHealth}/{maxHealth}");

        OnDamaged?.Invoke(damage, hitSource);

        if (IsDead)
        {
            if (enableDebugLogs) Debug.Log("[Health] Health reached zero - Dying");
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        if (enableDebugLogs) Debug.Log($"[Health] Healed! New HP: {currentHealth}/{maxHealth}");
        OnHealed?.Invoke();
    }

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        if (enableDebugLogs) Debug.Log($"[Health] Health set to {currentHealth}/{maxHealth}");
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        if (enableDebugLogs) Debug.Log($"[Health] Invulnerable set to {invulnerable}");
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }
}