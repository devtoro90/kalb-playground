// EnemyHealth.cs
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private bool invulnerable = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public bool Invulnerable { get => invulnerable; set => invulnerable = value; }

    public event Action<int, int> OnHealthChanged; // current, max
    public event Action OnDeath;
    public event Action OnDamaged;

    public void Initialize(EnemySettings settings)
    {
        maxHealth = settings.maxHealth;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (invulnerable || IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke();

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invulnerable = false;
    }
}