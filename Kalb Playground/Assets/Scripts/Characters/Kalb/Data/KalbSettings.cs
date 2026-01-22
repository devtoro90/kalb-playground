using UnityEngine;

[CreateAssetMenu(fileName = "KalbSettings", menuName = "KalbCharacter/Settings")]
public class KalbSettings : ScriptableObject
{
    [Header("Basic Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Friction")]
    public float groundFriction = 10f;
    public float airFriction = 2f;
    
    [Header("Jump & Air")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public float airControlMultiplier = 0.5f;
    public float maxAirSpeed = 10f;
    public float airAcceleration = 15f;
    
    [Header("Physics")]
    public float fallingGravityScale = 2.5f;
    public float normalGravityScale = 2f;
    public float quickFallGravityMultiplier = 1.2f;
    public float maxFallSpeed = -20f;
    
    [Header("Environment Detection")]
    public float groundCheckRadius = 0.2f;
    public LayerMask environmentLayer;
}