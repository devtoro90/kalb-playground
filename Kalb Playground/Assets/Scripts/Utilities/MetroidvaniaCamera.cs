using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum CameraFollowMode
{
    Basic,
    Advanced,
    HollowKnightStyle
}

[System.Serializable]
public enum CameraPriority
{
    PlayerPosition,
    PlayerVelocity,
    LookAhead,
    CustomTarget
}

[System.Serializable]
public class CameraUpgradeData
{
    public string upgradeName;
    public string description;
    public int cost;
    public bool unlocked = false;
}

public class MetroidvaniaCamera : MonoBehaviour
{
    // ====================================================================
    // SECTION 1: CORE SETTINGS
    // ====================================================================
    
    [Header("Core Settings")]
    public Transform player;
    public CameraFollowMode followMode = CameraFollowMode.Basic;
    public float cameraSpeed = 5f;
    
    [Header("Camera Boundaries")]
    public bool useCameraBounds = true;
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);
    public Collider2D cameraBoundsCollider;
    
    [Header("Screen Shake")]
    public float screenShakeIntensity = 0.05f;
    public float screenShakeDuration = 0.1f;
    public float screenShakeDamping = 1.0f;
    private Vector3 screenShakeOffset = Vector3.zero;
    private float screenShakeTimer = 0f;
    
    // ====================================================================
    // SECTION 2: BASIC FOLLOW (Unlocked by default)
    // ====================================================================
    
    [Header("Basic Follow")]
    [Tooltip("Deadzone radius where camera doesn't move")]
    public float deadzoneRadius = 0.5f;
    
    [Tooltip("How quickly camera responds to player movement")]
    public float responsiveness = 5f;

    [Header("Camera Smoothing")]
    public float cameraSmoothTime = 0.1f; // Lower = faster, Higher = slower
    public float maxCameraSpeed = 15f; // Limit maximum speed
    
    private Vector3 currentVelocity = Vector3.zero;    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Vector2 cameraHalfSize;
    
    // ====================================================================
    // SECTION 10: INITIALIZATION
    // ====================================================================
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Calculate camera half size in world units
        float height = cam.orthographicSize;
        float width = height * cam.aspect;
        cameraHalfSize = new Vector2(width, height);
        
        // Initialize target position
        if (player != null)
        {
            Vector3 playerPos = player.position;
            targetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
            transform.position = targetPosition;
        }
    }
    
    void FixedUpdate()
    {
        if (player == null) return;
        
        // Update screen shake
        UpdateScreenShake();
        
        // Calculate target position
        Vector3 playerPos = player.position;
        Vector3 cameraPos = transform.position;
        
        // Calculate distance from camera center
        float distance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.y),
            new Vector2(cameraPos.x, cameraPos.y)
        );
        
        // Only move if outside deadzone
        if (distance > deadzoneRadius)
        {
            targetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        }
        
        // Apply boundaries
        if (useCameraBounds)
        {
            ApplyBoundaries();
        }
        
        // Apply screen shake offset
        Vector3 finalTargetPosition = targetPosition + screenShakeOffset;
        
        // Smooth movement using SmoothDamp with max speed
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            finalTargetPosition, 
            ref currentVelocity, 
            cameraSmoothTime,
            maxCameraSpeed,
            Time.fixedDeltaTime
        );
        
        // Round to prevent sub-pixel movement (reduces blur)
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * 100f) / 100f;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * 100f) / 100f;
        
        transform.position = smoothedPosition;
    }
    
    // ====================================================================
    // SECTION 11: CORE FOLLOWING METHODS
    // ====================================================================
    
    private void BasicFollow()
    {
        Vector3 playerPos = player.position;
        Vector3 cameraPos = transform.position;
        
        // Calculate distance from camera center
        float distance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.y),
            new Vector2(cameraPos.x, cameraPos.y)
        );
        
        // Only move if outside deadzone
        if (distance > deadzoneRadius)
        {
            // Calculate target position directly
            targetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        }
        // Keep current position if within deadzone
    }
    
    // ====================================================================
    // SECTION 13: BOUNDARY SYSTEM - FIXED VERSION
    // ====================================================================
    
    private void ApplyBoundaries()
    {
        if (!useCameraBounds) return;
        
        // Calculate camera bounds in world space
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        
        // Calculate effective bounds (limiting camera edges, not center)
        float leftBound = minBounds.x + camWidth;
        float rightBound = maxBounds.x - camWidth;
        float bottomBound = minBounds.y + camHeight;
        float topBound = maxBounds.y - camHeight;
        
        // Clamp target position so camera edges stay within bounds
        targetPosition.x = Mathf.Clamp(targetPosition.x, leftBound, rightBound);
        targetPosition.y = Mathf.Clamp(targetPosition.y, bottomBound, topBound);
        
        // Debug visualization
        DebugDrawBounds(leftBound, rightBound, bottomBound, topBound);
    }
    
    private void DebugDrawBounds(float left, float right, float bottom, float top)
    {
        // Draw camera center bounds (what you had before)
        Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, minBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, maxBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, maxBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, minBounds.y, 0), Color.green);
        
        // Draw camera edge bounds (actual limits)
        Debug.DrawLine(new Vector3(left, bottom, 0), new Vector3(right, bottom, 0), Color.yellow);
        Debug.DrawLine(new Vector3(right, bottom, 0), new Vector3(right, top, 0), Color.yellow);
        Debug.DrawLine(new Vector3(right, top, 0), new Vector3(left, top, 0), Color.yellow);
        Debug.DrawLine(new Vector3(left, top, 0), new Vector3(left, bottom, 0), Color.yellow);
    }
    
    // ====================================================================
    // SECTION 14: SCREEN SHAKE SYSTEM
    // ====================================================================
    
    public void TriggerScreenShake(float intensity, float duration)
    {
        screenShakeIntensity = Mathf.Max(screenShakeIntensity, intensity);
        screenShakeDuration = Mathf.Max(screenShakeDuration, duration);
        screenShakeTimer = duration;
    }
    
    private void UpdateScreenShake()
    {
        if (screenShakeTimer > 0)
        {
            screenShakeTimer -= Time.deltaTime;
            
            float currentIntensity = screenShakeIntensity * (screenShakeTimer / screenShakeDuration);
            screenShakeOffset = Random.insideUnitSphere * currentIntensity;
            screenShakeOffset.z = 0; // Keep shake in 2D plane
        }
        else
        {
            screenShakeOffset = Vector3.zero;
        }
    }
    
    // ====================================================================
    // SECTION 18: DEBUG & VISUALIZATION
    // ====================================================================
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // In editor, approximate camera size
            Camera editorCam = GetComponent<Camera>();
            if (editorCam == null) editorCam = Camera.main;
            
            if (editorCam != null)
            {
                float camHeight = editorCam.orthographicSize;
                float camWidth = camHeight * editorCam.aspect;
                
                // Draw camera edge bounds
                if (useCameraBounds)
                {
                    float leftBound = minBounds.x + camWidth;
                    float rightBound = maxBounds.x - camWidth;
                    float bottomBound = minBounds.y + camHeight;
                    float topBound = maxBounds.y - camHeight;
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(
                        new Vector3((leftBound + rightBound) * 0.5f, (bottomBound + topBound) * 0.5f, 0),
                        new Vector3(rightBound - leftBound, topBound - bottomBound, 0)
                    );
                }
            }
        }
        
        // Draw original bounds
        if (useCameraBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0),
                new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0)
            );
        }
        
        // Draw deadzone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deadzoneRadius);
    }
    
    // Private fields that were missing
    private Vector3 targetPosition;
    private Vector3 smoothedPosition;
}