// EnemyHealth.cs
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private bool invulnerable = false;
    [SerializeField] private bool immortal = false; // New: takes damage but never dies

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => !immortal && currentHealth <= 0; // Can't die if immortal
    public bool Invulnerable { get => invulnerable; set => invulnerable = value; }
    public bool Immortal { get => immortal; set => immortal = value; }

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
        if (invulnerable || (IsDead && !immortal)) return;

        // Store previous health to check if we actually changed
        int previousHealth = currentHealth;

        // Reduce health if not immortal, but still trigger events if immortal to allow for hit reactions
        if (!immortal)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
        }

        // Only trigger events if health actually changed or if immortal (to allow for hit reactions without death)
        if (previousHealth != currentHealth || immortal)
        {
            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Only trigger death if not immortal and health is 0
            if (!immortal && currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
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

    public void SetImmortal(bool value)
    {
        immortal = value;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invulnerable = false;
        immortal = false;
    }
}