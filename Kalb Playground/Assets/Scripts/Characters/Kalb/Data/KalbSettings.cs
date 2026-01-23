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
    
    [Header("Swimming Settings")]
    public float swimSpeed = 3f;
    public float swimFastSpeed = 5f;
    public float swimDashSpeed = 10f;
    public float swimJumpForce = 8f;
    public float waterSurfaceOffset = 1.20f;
    public float waterEntrySpeedReduction = 0.5f;
    public LayerMask waterLayer;
    public float waterCheckRadius = 0.5f;
    public float waterEntryGravity = 0.5f;
    public float buoyancyStrength = 50f;
    public float buoyancyDamping = 10f;
    public float maxBuoyancyForce = 20f;
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1f;
    public float floatSmoothness = 5f;
    public bool enableFloating = true;
    public float swimDashDuration = 0.15f;
    public float swimDashCooldown = 0.3f;

    [Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool doubleJumpUnlocked = false;
    public bool wallLockUnlocked = false;
    public bool pogoUnlocked = false;
}