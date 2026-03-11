using UnityEngine;

public class KalbParticleController : MonoBehaviour
{
    [Header("Kalb References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private KalbSettings settings;

    [Header("Particle System References")]
    [SerializeField] private ParticleSystem runningDustSystem;
    [SerializeField] private ParticleSystem jumpDustSystem;
    [SerializeField] private ParticleSystem wallSlideDustSystem;
    [SerializeField] private ParticleSystem landingDustSystem;
    [SerializeField] private ParticleSystem dashTrailSystem;

    [Header("Running Dust Settings")]
    [SerializeField] private float minEmissionRate = 5f;    // At minimum speed
    [SerializeField] private float maxEmissionRate = 30f;   // At maximum speed
    [SerializeField] private float minSpeedThreshold = 5.0f; // Minimum speed to emit
    [SerializeField] private float dustSpawnOffset = 0.5f;   // How far below center to spawn

    [Header("Wall Slide Dust")]
    [SerializeField] private float wallSlideEmissionRate = 20f;
    [SerializeField] private float wallSlideDustOffset = 0.4f; // Distance from center to wall
    [SerializeField] private float wallSlideVerticalOffset = 0.2f;

    [Header("Dash Trail Particles")]
    [SerializeField] private float dashTrailEmissionRate = 30f;


    [Header("Pooling")]
    [SerializeField] private KalbParticlePool particlePool;
    [SerializeField] private string runningDustPoolName = "RunningDust";
    [SerializeField] private string jumpDustPoolName = "JumpDust";
    [SerializeField] private string wallSlideDustPoolName = "WallSlideDust";
    [SerializeField] private string landingDustPoolName = "LandingDust";
    [SerializeField] private string dashTrailPoolName = "DashTrail";
    [SerializeField] private string dashLinePoolName = "DashLineTrail";

    // Cache the emission module for performance
    private ParticleSystem.EmissionModule runningEmission;
    private ParticleSystem.EmissionModule wallSlideEmission;

    private void Awake()
    {
        // Cache the emission module
        if (runningDustSystem != null)
            runningEmission = runningDustSystem.emission;

        if (wallSlideDustSystem != null)
            wallSlideEmission = wallSlideDustSystem.emission;
    }

    private void Start()
    {
        // Ensure particle systems are positioned at feet
        if (runningDustSystem != null)
            PositionParticleSystemAtFeet(runningDustSystem);

        if (jumpDustSystem != null)
            PositionParticleSystemAtFeet(jumpDustSystem);
    }

    /// <summary>
    /// Positions a particle system at the player's feet
    /// WHY: Dust should emit from feet, not center of player
    /// </summary>
    private void PositionParticleSystemAtFeet(ParticleSystem ps)
    {
        // Get the player's collider to calculate foot position
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            // Calculate foot position (bottom of collider)
            float footY = playerCollider.bounds.min.y;
            float playerCenterY = playerCollider.bounds.center.y;

            // Convert to local position relative to player
            Vector3 localFootPosition = new Vector3(
                0,
                footY - playerCenterY, // Negative value (below center)
                0
            );

            // Set the particle system's local position
            ps.transform.localPosition = localFootPosition;
        }
    }

    /// <summary>
    /// Controls running dust based on movement
    /// Called from KalbMovement when on ground
    /// </summary>
    public void UpdateRunningDust(float horizontalSpeed, bool isGrounded)
    {
        Debug.Log($"[Particle Debug] Speed: {horizontalSpeed}, Grounded: {isGrounded}, " +
              $"System exists: {runningDustSystem != null}, IsPlaying: {runningDustSystem?.isPlaying}");

        if (runningDustSystem == null) return;

        if (isGrounded && Mathf.Abs(horizontalSpeed) > minSpeedThreshold)
        {

            // Calculate emission rate based on speed
            float speedRatio = Mathf.Abs(horizontalSpeed) / settings.runSpeed;
            float emissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, speedRatio);

            // Apply to particle system
            runningEmission.rateOverTime = emissionRate;

            // Make sure it's playing
            if (!runningDustSystem.isPlaying)
            {

                if (particlePool != null)
                {
                    Vector3 footPosition = GetFootPosition();
                    particlePool.GetParticle(runningDustPoolName, footPosition, Quaternion.identity);
                }
                else
                {
                    // Fallback to direct play
                    runningDustSystem.Play();
                }
            }
        }
        else
        {
            // Stop emitting when not moving or in air
            runningEmission.rateOverTime = 0;

            // Optional: Let existing particles finish naturally
            // Don't call Stop() immediately - let them fade out
        }
    }

    /// <summary>
    /// Positions wall slide dust at the correct wall contact point
    /// WHY: Dust should appear where the player touches the wall, not at center
    /// </summary>
    private void PositionWallSlideDust()
    {
        if (wallSlideDustSystem == null || controller.WallJump == null) return;

        // Get wall side (-1 for left, 1 for right)
        int wallSide = controller.WallJump.WallSide;
        if (wallSide == 0) return; // Not touching wall

        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            // Calculate position at hand level (slightly above center)
            float handY = playerCollider.bounds.center.y + wallSlideVerticalOffset;
            float playerCenterX = playerCollider.bounds.center.x;

            // Position at wall contact point
            float targetX = playerCenterX + (wallSide * wallSlideDustOffset);

            // Convert to local position
            Vector3 localPosition = transform.InverseTransformPoint(new Vector3(
                targetX,
                handY,
                -0.05f // Slight Z offset
            ));

            wallSlideDustSystem.transform.localPosition = localPosition;

            // Adjust shape to emit toward wall/downward
            var shape = wallSlideDustSystem.shape;
            shape.angle = 90; // Emit downward
            shape.rotation = new Vector3(0, 0, wallSide == 1 ? 0 : 180); // Flip for left wall


        }
    }

    /// <summary>
    /// Updates wall slide dust position and emission based on wall sliding state
    /// Called from KalbWallSlideState
    /// </summary>
    public void UpdateWallSlideDust(bool isWallSliding, int wallSide, float slideSpeed)
    {

        if (wallSlideDustSystem == null) return;


        if (isWallSliding && wallSide != 0)
        {
            // Update position (wall side might change)
            PositionWallSlideDust();

            // Adjust emission rate based on slide speed
            float speedRatio = Mathf.Abs(slideSpeed) / 8f; // Normalize to max slide speed
            float emissionRate = Mathf.Lerp(10f, 30f, speedRatio);
            wallSlideEmission.rateOverTime = emissionRate;


            // Play if not already playing
            if (!wallSlideDustSystem.isPlaying)
            {


                if (particlePool != null)
                {
                    Vector3 wallSlidePosition = GetWallSlidePosition(wallSide);
                    particlePool.GetParticle(wallSlideDustPoolName, wallSlidePosition, Quaternion.identity);
                }
                else
                {
                    // Fallback to direct play
                    wallSlideDustSystem.Play();
                }
            }
        }
        else
        {
            // Stop emitting when not wall sliding
            wallSlideEmission.rateOverTime = 0;
        }
    }

    /// <summary>
    /// Triggers jump dust effect
    /// Called from KalbJumpState when jumping
    /// </summary>
    public void PlayJumpDust()
    {
        if (particlePool != null)
        {
            Vector3 footPosition = GetFootPosition();
            particlePool.GetParticle(jumpDustPoolName, footPosition, Quaternion.identity);
        }
        else
        {
            // Fallback to direct play
            jumpDustSystem.Play();
        }
    }

    /// <summary>
    /// Triggers landing dust effect
    /// Called from KalbJumpState when jumping
    /// </summary>
    public void PlayLandingDust()
    {
        if (particlePool != null)
        {
            Vector3 footPosition = GetFootPosition();
            particlePool.GetParticle(landingDustPoolName, footPosition, Quaternion.identity);
        }
        else
        {
            // Fallback to direct play
            landingDustSystem.Play();
        }
    }


    public void StartDashTrailParticles(Vector2 dashDirection)
    {
        if (dashTrailSystem == null) return;

        // Position trail system behind player
        PositionDashTrailSystem(dashDirection);

        // Set emission rate
        var emission = dashTrailSystem.emission;
        emission.rateOverTime = dashTrailEmissionRate;

        if (!dashTrailSystem.isPlaying)
        {
            dashTrailSystem.Play();
        }


    }

    public void StopDashTrailParticles()
    {
        if (dashTrailSystem == null) return;

        var emission = dashTrailSystem.emission;
        emission.rateOverTime = 0;


    }

    private void PositionDashTrailSystem(Vector2 dashDirection)
    {
        if (dashTrailSystem == null) return;

        // Position trail system behind player relative to dash direction
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            Vector3 playerCenter = playerCollider.bounds.center;

            // Place trail slightly behind the dash direction
            Vector3 offset = -dashDirection.normalized * 0.3f;
            Vector3 trailPosition = playerCenter + offset;

            dashTrailSystem.transform.position = trailPosition;

            // Rotate trail to emit backward relative to dash direction
            float angle = GetAngleFromDirection(-dashDirection);
            dashTrailSystem.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private float GetAngleFromDirection(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    Vector3 GetFootPosition()
    {
        Collider2D col = GetComponent<Collider2D>();
        float footY = col.bounds.min.y + 0.5f;  // Bottom of collider
        float footX = transform.position.x;  // Centered horizontally
        return new Vector3(footX, footY, transform.position.z);
    }

    Vector3 GetWallSlidePosition(int wallSide)
    {
        Collider2D col = GetComponent<Collider2D>();
        float footY = col.bounds.max.y - 0.2f;  // Bottom of collider
        float footX = transform.position.x + (wallSide * 0.5f);  // Centered horizontally
        return new Vector3(footX, footY, transform.position.z);
    }

    /// <summary>
    /// Updates particle positions when player flips
    /// Called from KalbMovement when FacingRight changes
    /// </summary>
    public void UpdateFacingDirection(bool facingRight)
    {
        // Particles don't need to flip - they're in world space
        // But we might want to flip the shape direction for running dust

        if (runningDustSystem != null)
        {
            var shape = runningDustSystem.shape;
            // If you want dust to trail behind, you'd adjust angle based on direction
            // shape.angle = facingRight ? 5 : 175;
        }
        // Also update wall slide dust if active
        if (wallSlideDustSystem != null && controller.WallJump != null && controller.WallJump.IsWallSliding)
        {
            PositionWallSlideDust();
        }

        // Also update dash trail orientation if active
        if (dashTrailSystem != null && dashTrailSystem.isPlaying && controller.DashState != null)
        {
            PositionDashTrailSystem(controller.DashState.DashDirection);
        }
    }

    /// <summary>
    /// Clean up particles when disabled
    /// </summary>
    private void OnDisable()
    {
        // Stop all particle systems immediately
        if (runningDustSystem != null)
            runningDustSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (jumpDustSystem != null)
            jumpDustSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnDrawGizmosSelected()
    {
        if (runningDustSystem != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(runningDustSystem.transform.position, 0.2f);
        }

        if (jumpDustSystem != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(jumpDustSystem.transform.position, 0.2f);
        }
    }


}