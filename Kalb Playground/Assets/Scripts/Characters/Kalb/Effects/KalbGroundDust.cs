// KalbGroundDust.cs
using UnityEngine;

public class KalbGroundDust : MonoBehaviour
{
    [Header("Dust Settings")]
    [SerializeField] private float minSpeedForDust = 5f;
    [SerializeField] private float dustSpawnInterval = 0.1f;
    [SerializeField] private AnimationCurve dustSizeBySpeed = AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f);
    
    [Header("References")]
    [SerializeField] private ParticleSystem dustParticles;
    [SerializeField] private TrailRenderer dustTrail;
    
    private KalbController controller;
    private KalbMovement movement;
    private float dustTimer;
    
    private void Awake()
    {
        controller = GetComponentInParent<KalbController>();
        movement = GetComponentInParent<KalbMovement>();
        
        if (dustParticles == null)
            dustParticles = GetComponent<ParticleSystem>();
    }
    
    private void Update()
    {
        if (controller == null) return;
        
        bool shouldEmit = ShouldEmitDust();
        
        if (shouldEmit)
        {
            UpdateDustEmission();
        }
        else
        {
            StopDust();
        }
    }
    
    private bool ShouldEmitDust()
    {
        if (!controller.IsEffectivelyGrounded()) return false;
        
        float speed = Mathf.Abs(controller.Rb.linearVelocity.x);
        return speed >= minSpeedForDust;
    }
    
    private void UpdateDustEmission()
    {
        float speedRatio = GetSpeedRatio();
        
        // Adjust particle size based on speed
        if (dustParticles != null)
        {
            var main = dustParticles.main;
            main.startSizeMultiplier = dustSizeBySpeed.Evaluate(speedRatio);
        }
        
        // Adjust trail width
        if (dustTrail != null)
        {
            dustTrail.widthMultiplier = dustSizeBySpeed.Evaluate(speedRatio);
        }
        
        // Ensure particles are playing
        if (dustParticles != null && !dustParticles.isPlaying)
            dustParticles.Play();
            
        if (dustTrail != null && !dustTrail.emitting)
            dustTrail.emitting = true;
    }
    
    private void StopDust()
    {
        if (dustParticles != null && dustParticles.isPlaying)
            dustParticles.Stop();
            
        if (dustTrail != null && dustTrail.emitting)
            dustTrail.emitting = false;
    }
    
    private float GetSpeedRatio()
    {
        float speed = Mathf.Abs(controller.Rb.linearVelocity.x);
        float maxSpeed = controller.Settings?.runSpeed ?? 10f;
        return Mathf.Clamp01(speed / maxSpeed);
    }
}