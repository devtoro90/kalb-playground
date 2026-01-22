using UnityEngine;

public class KalbHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        
        if (IsDead)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
    
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
    
    private void Die()
    {
        Debug.Log("Kalb died!");
        // Add death logic here
    }
}