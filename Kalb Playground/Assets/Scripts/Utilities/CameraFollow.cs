using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Dynamic Smoothing")]
    public float minSmoothSpeed = 0.1f;
    public float maxSmoothSpeed = 0.5f;
    public float maxDistance = 5f; // Distance at which camera moves fastest
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.position + offset;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Dynamic speed based on distance
        float dynamicSpeed = Mathf.Lerp(minSmoothSpeed, maxSmoothSpeed, distance / maxDistance);
        
        // Use Lerp with dynamic speed
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            dynamicSpeed * Time.deltaTime * 10f
        );
        
        smoothedPosition.z = offset.z;
        transform.position = smoothedPosition;
    }
}