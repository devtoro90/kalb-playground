using UnityEngine;
using UnityEngine.Events;

public class SpikeTile : MonoBehaviour
{
    [Header("Damage Settings")]
    public int spikeDamage = 20;
    public float knockbackForce = 10f;
    public Vector2 knockbackDirection = new Vector2(0, 10f);
    
    [Header("Pogo Settings")]
    public bool pogoEnabled = true;
    public float pogoBounceForce = 15f;
    public float pogoBounceCooldown = 0.2f;
    public bool breakAfterPogo = false;
    public GameObject breakEffect;
    
    [Header("Visual Feedback")]
    public bool flashOnHit = true;
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;
    
    [Header("Events")]
    public UnityEvent onPlayerHit;
    public UnityEvent onPogoHit;
    public UnityEvent onTileBreak;
    
    // Internal state
    private float lastPogoTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Collider2D tileCollider;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tileCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other.gameObject);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }
    
    private void HandlePlayerCollision(GameObject player)
    {
        Kalb playerController = player.GetComponent<Kalb>();
        
        if (playerController != null)
        {
            // Check if player is pogo attacking (downward attack)
            if (playerController.IsPogoAttacking() && pogoEnabled)
            {
                HandlePogoCollision(playerController);
            }
            else
            {
                HandleDamageCollision(playerController);
            }
        }
    }
    
    private void HandleDamageCollision(Kalb player)
    {
        // Apply damage to player (you'll need to implement this in your player health system)
        // player.TakeDamage(spikeDamage);
        
        // Apply knockback
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = new Vector2(0, 0); // Reset velocity
            playerRb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }
        
        // Visual feedback
        if (flashOnHit && spriteRenderer != null)
        {
            StartCoroutine(FlashSprite());
        }
        
        // Trigger events
        onPlayerHit.Invoke();
    }
    
    private void HandlePogoCollision(Kalb player)
    {
        // Check cooldown
        if (Time.time - lastPogoTime < pogoBounceCooldown)
            return;
        
        lastPogoTime = Time.time;
        
        // Apply pogo bounce to player
        player.ApplyPogoBounce(pogoBounceForce);
        
        // Visual feedback
        if (flashOnHit && spriteRenderer != null)
        {
            StartCoroutine(FlashSprite());
        }
        
        // Break tile if configured
        if (breakAfterPogo)
        {
            BreakTile();
        }
        
        // Trigger events
        onPogoHit.Invoke();
    }
    
    private System.Collections.IEnumerator FlashSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    private void BreakTile()
    {
        // Spawn break effect
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }
        
        // Disable collider and renderer
        if (tileCollider != null) tileCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        // Trigger event
        onTileBreak.Invoke();
        
        // Destroy after effects
        Destroy(gameObject, 2f);
    }
    
    // Public method for external pogo detection
    public bool CanBePogoed()
    {
        return pogoEnabled && (Time.time - lastPogoTime >= pogoBounceCooldown);
    }
}