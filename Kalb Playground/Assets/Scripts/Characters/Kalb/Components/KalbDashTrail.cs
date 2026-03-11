// KalbDashTrail.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KalbDashTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Organization")]
    [SerializeField] private Transform trailContainer;
    [SerializeField] private bool useWorldSpace = true; // NEW: Control positioning mode

    [Header("Trail Settings")]
    [SerializeField] private int maxTrailCount = 10;
    [SerializeField] private float trailSpawnInterval = 0.03f;
    [SerializeField] private float trailLifetime = 0.3f;
    [SerializeField] private Material trailMaterial;

    [Header("Trail Appearance")]
    [SerializeField] private Color trailStartColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color trailEndColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float trailScale = 1f;
    [SerializeField] private bool fadeOut = true;

    [Header("Direction-Based Effects")]
    [SerializeField] private bool rotateTrailsToDirection = true;
    [SerializeField] private float rotationSmoothness = 5f;

    [Header("Performance")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int prewarmCount = 10;

    [Header("Direction-Based Effects")]
    [SerializeField] private bool useDirectionColors = true;
    [SerializeField] private Color horizontalDashColor = Color.white;
    [SerializeField] private Color verticalDashColor = Color.cyan;
    [SerializeField] private Color diagonalDashColor = new Color(0.8f, 0.8f, 1f, 1f);



    // Trail management
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private List<TrailInstance> activeTrails = new List<TrailInstance>();
    private float lastSpawnTime;
    private bool isTrailActive = false;

    // Trail instance class
    private class TrailInstance
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public float creationTime;
        public float lifetime;
        public Vector3 startScale;
        public Color startColor;
        public Color endColor;
        public bool fadeOut;
    }

    private void Start()
    {
        if (controller == null) controller = GetComponent<KalbController>();
        if (playerSprite == null) playerSprite = GetComponent<SpriteRenderer>();

        // Create a container for trail objects
        if (trailContainer == null)
        {
            GameObject container = new GameObject("DashTrailContainer");
            trailContainer = container.transform;
            trailContainer.SetParent(transform);
            trailContainer.localPosition = Vector3.zero;

            // FIX: Ensure container doesn't interfere with rendering
            trailContainer.gameObject.layer = gameObject.layer;
        }

        if (useObjectPooling)
        {
            PrewarmPool();
        }
    }

    private GameObject CreateTrailObject()
    {
        GameObject trailObj = new GameObject("DashTrail");

        // FIX: Set parent but preserve world position
        if (useWorldSpace)
        {
            // For world space trails, don't parent (or parent to scene root)
            trailObj.transform.SetParent(null); // No parent = world space
        }
        else
        {
            // For local space trails, parent to container
            trailObj.transform.SetParent(trailContainer);
        }

        // Ensure position is set correctly
        trailObj.transform.position = transform.position;
        trailObj.transform.rotation = transform.rotation;

        SpriteRenderer renderer = trailObj.AddComponent<SpriteRenderer>();
        renderer.sprite = playerSprite.sprite;

        // FIX: Critical - ensure sorting layer and order are correct
        renderer.sortingLayerID = playerSprite.sortingLayerID;
        renderer.sortingOrder = playerSprite.sortingOrder - 1; // Behind player

        // FIX: Ensure the trail is visible
        renderer.enabled = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (trailMaterial != null)
        {
            renderer.material = trailMaterial;
        }

        // Add a temporary visibility helper (optional)
#if UNITY_EDITOR
        trailObj.name = $"DashTrail_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
#endif

        return trailObj;
    }

    private void SpawnTrailInstance()
    {
        if (playerSprite == null || playerSprite.sprite == null) return;

        // Get trail object
        GameObject trailObj = GetTrailObject();

        if (trailObj == null) return;

        // FIX: Force world position to match player exactly
        trailObj.transform.position = transform.position;
        trailObj.transform.rotation = transform.rotation;

        // Set sprite
        SpriteRenderer trailRenderer = trailObj.GetComponent<SpriteRenderer>();
        trailRenderer.sprite = playerSprite.sprite;
        trailRenderer.flipX = playerSprite.flipX;
        trailRenderer.flipY = playerSprite.flipY;

        // FIX: Ensure renderer is enabled
        trailRenderer.enabled = true;

        // Scale
        trailObj.transform.localScale = transform.localScale * trailScale;

        // FIX: If using world space, ensure we're not scaling weirdly
        if (useWorldSpace)
        {
            trailObj.transform.localScale = trailObj.transform.localScale;
        }

        // Create trail instance
        TrailInstance instance = new TrailInstance
        {
            gameObject = trailObj,
            renderer = trailRenderer,
            creationTime = Time.time,
            lifetime = trailLifetime,
            startScale = trailObj.transform.localScale,
            startColor = trailStartColor,
            endColor = trailEndColor,
            fadeOut = fadeOut
        };

        trailRenderer.color = trailStartColor;
        activeTrails.Add(instance);
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            GameObject trailObj = CreateTrailObject();
            trailObj.SetActive(false);
            trailPool.Enqueue(trailObj);
        }
    }

    private GameObject GetTrailObject()
    {
        if (useObjectPooling && trailPool.Count > 0)
        {
            GameObject obj = trailPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return CreateTrailObject();
        }
    }

    private void ReturnTrailObject(GameObject obj)
    {
        if (useObjectPooling)
        {
            obj.SetActive(false);
            trailPool.Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    public void StartDashTrail()
    {
        isTrailActive = true;
        lastSpawnTime = -trailSpawnInterval; // Spawn immediately

        // Play dash start effect
        SpawnDashStartParticles();
    }

    public void StopDashTrail()
    {
        isTrailActive = false;

        ForceCleanupTrails();
    }

    public void ForceCleanupTrails()
    {
        // Immediately destroy all active trails
        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            TrailInstance trail = activeTrails[i];
            if (trail.gameObject != null)
            {
                if (useObjectPooling)
                {
                    trail.gameObject.SetActive(false);
                    trailPool.Enqueue(trail.gameObject);
                }
                else
                {
                    Destroy(trail.gameObject);
                }
            }
        }
        activeTrails.Clear();

        Debug.Log($"[DashTrail] Force cleaned up {activeTrails.Count} trails");
    }

    private void Update()
    {
        Debug.Log($"[DashTrail] Update - isTrailActive: {isTrailActive}, ActiveTrails: {activeTrails.Count}");

        // Safety cleanup - if dash is no longer active but trails remain
        if (!isTrailActive && activeTrails.Count > 0)
        {
            // Check if controller's dash state is still active
            if (controller != null && controller.DashState != null && !controller.DashState.IsDashing)
            {
                Debug.Log("[DashTrail] Dash ended, cleaning up trails");
                ForceCleanupTrails();
            }
        }

        if (!isTrailActive)
        {
            // Still need to update existing trails even if not spawning new ones
            UpdateActiveTrails();
            return;
        }

        // Spawn new trail instances
        if (Time.time >= lastSpawnTime + trailSpawnInterval)
        {
            Debug.Log("[DashTrail] Spawning new trail instance");
            SpawnTrailInstance();
            lastSpawnTime = Time.time;
        }

        // Update existing trails
        UpdateActiveTrails();
    }

    private void UpdateActiveTrails()
    {
        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            TrailInstance trail = activeTrails[i];

            // Safety check - if game object was destroyed
            if (trail.gameObject == null)
            {
                activeTrails.RemoveAt(i);
                continue;
            }

            float age = Time.time - trail.creationTime;
            float lifeRatio = age / trail.lifetime;

            if (lifeRatio >= 1f)
            {
                // Trail expired
                Debug.Log($"[DashTrail] Trail expired, returning to pool");
                ReturnTrailObject(trail.gameObject);
                activeTrails.RemoveAt(i);
                continue;
            }

            if (trail.fadeOut)
            {
                // Fade out using color lerp
                Color newColor = Color.Lerp(trail.startColor, trail.endColor, lifeRatio);
                trail.renderer.color = newColor;
            }
        }
    }


    private void SpawnDashStartParticles()
    {
        // Get dash direction
        Vector2 dashDir = controller.DashState != null ?
            controller.DashState.DashDirection :
            (controller.FacingRight ? Vector2.right : Vector2.left);

        // Spawn burst particles using existing particle system
        if (controller.ParticleController != null)
        {
            // You can call a method on ParticleController to spawn dash burst
            // We'll implement this later
        }
    }

    private void OnDisable()
    {
        // Clean up all trails
        StopDashTrail();

        foreach (var trail in activeTrails)
        {
            if (trail.gameObject != null)
            {
                ReturnTrailObject(trail.gameObject);
            }
        }
        activeTrails.Clear();
    }
}