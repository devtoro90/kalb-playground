// EnemySettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySettings", menuName = "Enemy/EnemySettings")]
public class EnemySettings : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 100;
    public bool destroyOnDeath = true;
    public float deathDelay = 0.5f;

    [Header("Combat")]
    public int contactDamage = 10;
    public float knockbackResistance = 1f; // Multiplier for knockback received

    [Header("Physics")]
    public float gravityScale = 2f;
    public bool freezeRotation = true;

    [Header("Knockback")]
    public float knockbackDeceleration = 10f; // How quickly knockback wears off
    public bool useKnockbackDirectionOverride = false;
    public Vector2 knockbackDirectionOverride = Vector2.up;

    [Header("Visuals")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;
    public int hitFlashCount = 1;

    [Header("Death Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;
    public float deathEffectDuration = 1f;
}