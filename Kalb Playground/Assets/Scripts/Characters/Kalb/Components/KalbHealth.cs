// KalbHealth.cs - Add debug logging
using UnityEngine;

public class KalbHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private bool isInvulnerable = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => isInvulnerable;

    public System.Action<int, Vector2> OnDamaged;
    public System.Action OnHealed;
    public System.Action OnDeath;

    private void Start()
    {

    }

    public void TakeDamage(int damage, Vector2 hitSource)
    {


        if (IsDead)
        {

            return;
        }

        if (isInvulnerable)
        {

            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);


        OnDamaged?.Invoke(damage, hitSource);

        if (IsDead)
        {

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

        OnHealed?.Invoke();
    }

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);

    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;

    }

    private void Die()
    {
        OnDeath?.Invoke();
    }
}