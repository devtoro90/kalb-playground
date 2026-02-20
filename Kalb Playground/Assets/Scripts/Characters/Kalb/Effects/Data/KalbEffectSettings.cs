// KalbEffectSettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "KalbEffectSettings", menuName = "KalbCharacter/Effect Settings")]
public class KalbEffectSettings : ScriptableObject
{
    [Header("Ground Dust")]
    public GameObject groundDustPrefab;
    public float dustSpawnOffsetY = 0.1f;
    public float dustSpawnDistance = 0.5f;
    
    [Header("Run Dust")]
    public float minRunSpeedForDust = 8f;
    public float dustEmissionRate = 20f;
    public AnimationCurve emissionRateBySpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    
    [Header("Landing Dust")]
    public GameObject landingDustPrefab;
    public float minLandingVelocity = -5f;
    public float landingDustDuration = 0.5f;
    
    [Header("Wall Dust")]
    public GameObject wallDustPrefab;
    public float wallSlideEmissionRate = 10f;
    
    [Header("Pooling")]
    public int poolSize = 5;
    public bool useObjectPooling = true;
}