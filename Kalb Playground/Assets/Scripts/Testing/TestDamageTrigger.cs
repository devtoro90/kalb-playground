using UnityEngine;

public class TestDamageTrigger : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 10;
    public float knockbackForce = 12f;
    public bool continuousDamage = false;
    public float damageInterval = 1f;
    
    [Header("Visual Feedback")]
    public Color triggerColor = Color.red;
    public bool showDebug = true;
    
    private float lastDamageTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = triggerColor;
        }
        
        if (showDebug)
            Debug.Log($"TestDamageTrigger ready. Damage: {damageAmount}, Knockback: {knockbackForce}");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDamage(other.gameObject);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (continuousDamage && other.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime >= damageInterval)
            {
                ApplyDamage(other.gameObject);
                lastDamageTime = Time.time;
            }
        }
    }
    
    private void ApplyDamage(GameObject player)
    {
        Kalb playerController = player.GetComponent<Kalb>();
        if (playerController != null)
        {
            playerController.TakeDamage(damageAmount, transform.position, knockbackForce);
            
            if (showDebug)
                Debug.Log($"Applied {damageAmount} damage to player from {transform.position}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebug)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                if (collider is BoxCollider2D)
                {
                    BoxCollider2D box = collider as BoxCollider2D;
                    Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
                }
                else if (collider is CircleCollider2D)
                {
                    CircleCollider2D circle = collider as CircleCollider2D;
                    Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                }
            }
        }
    }
}