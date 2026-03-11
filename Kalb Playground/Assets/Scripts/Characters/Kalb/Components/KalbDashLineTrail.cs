// DashLineTrail.cs
using System.Collections.Generic;
using UnityEngine;

public class KalbDashLineTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Line Settings - GLOWING LIGHTSABER LINES")]
    [SerializeField] private int lineCount = 5;
    [SerializeField] private float lineLength = 0.5f;
    [SerializeField] private float lineWidth = 0.03f;
    [SerializeField] private float lineSpread = 0.8f;
    [SerializeField] private Material lineMaterial;

    [Header("Glow Settings")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color glowColor = new Color(1f, 1f, 0f, 0.4f);
    [SerializeField] private float coreWidthMultiplier = 0.6f;
    [SerializeField] private float glowWidthMultiplier = 1f;
    [SerializeField] private bool usePulseGlow = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseIntensity = 0.2f;

    [Header("Fade Settings")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool fadeWidth = false;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.05f; // Spawn lines every 0.05 seconds
    private float lastSpawnTime;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private class GlowingLine
    {
        public LineRenderer coreRenderer;
        public LineRenderer glowRenderer;
        public float spawnTime;
        public Vector3 startPos;  // Fixed start position when spawned
        public Vector3 endPos;     // Fixed end position when spawned
        public float baseCoreWidth;
        public float baseGlowWidth;
    }

    private List<GlowingLine> activeLines = new List<GlowingLine>();
    private Queue<LineRenderer> coreLinePool = new Queue<LineRenderer>();
    private Queue<LineRenderer> glowLinePool = new Queue<LineRenderer>();
    private bool isDashing = false;
    private Vector2 dashDirection;
    private float dashDuration;
    private Transform lineContainer;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<KalbController>();
        if (playerSprite == null) playerSprite = GetComponent<SpriteRenderer>();

        GameObject container = new GameObject("DashLineContainer");
        lineContainer = container.transform;
        lineContainer.SetParent(transform);
        lineContainer.localPosition = Vector3.zero;

        for (int i = 0; i < 30; i++)
        {
            CreateCoreLineRenderer();
            CreateGlowLineRenderer();
        }
    }

    private LineRenderer CreateCoreLineRenderer()
    {
        GameObject lineObj = new GameObject("DashLine_Core");
        lineObj.transform.SetParent(lineContainer);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;

        if (lineMaterial != null)
            line.material = lineMaterial;
        else
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.renderQueue = 3000;
        }

        line.material.EnableKeyword("_ALPHABLEND_ON");
        line.startWidth = lineWidth * coreWidthMultiplier;
        line.endWidth = lineWidth * coreWidthMultiplier;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.sortingOrder = 1001;
        line.sortingLayerName = "Default";
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        lineObj.SetActive(false);
        coreLinePool.Enqueue(line);
        return line;
    }

    private LineRenderer CreateGlowLineRenderer()
    {
        GameObject lineObj = new GameObject("DashLine_Glow");
        lineObj.transform.SetParent(lineContainer);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;

        if (lineMaterial != null)
            line.material = lineMaterial;
        else
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.renderQueue = 2999;
        }

        line.material.EnableKeyword("_ALPHABLEND_ON");
        line.startWidth = lineWidth * glowWidthMultiplier;
        line.endWidth = lineWidth * glowWidthMultiplier;
        line.startColor = glowColor;
        line.endColor = glowColor;
        line.sortingOrder = 1000;
        line.sortingLayerName = "Default";
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        lineObj.SetActive(false);
        glowLinePool.Enqueue(line);
        return line;
    }

    public void StartDashLines(Vector2 direction, float duration)
    {
        isDashing = true;
        dashDirection = direction.normalized;
        dashDuration = duration;
        lastSpawnTime = Time.time; // Reset spawn timer

        ClearAllLines();
    }

    public void StopDashLines()
    {
        isDashing = false;
    }

    private void Update()
    {
        if (!isDashing && activeLines.Count == 0)
            return;

        float currentTime = Time.time;

        // Spawn new lines at intervals while dashing
        if (isDashing && currentTime - lastSpawnTime >= spawnInterval)
        {
            SpawnLineSet();
            lastSpawnTime = currentTime;
        }

        // Update existing lines - with stretching
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            GlowingLine line = activeLines[i];

            if (line.coreRenderer == null || line.glowRenderer == null)
            {
                activeLines.RemoveAt(i);
                continue;
            }

            float age = currentTime - line.spawnTime;
            float lifeRatio = age / dashDuration;

            if (lifeRatio >= 1f)
            {
                line.coreRenderer.gameObject.SetActive(false);
                line.glowRenderer.gameObject.SetActive(false);
                coreLinePool.Enqueue(line.coreRenderer);
                glowLinePool.Enqueue(line.glowRenderer);
                activeLines.RemoveAt(i);
            }
            else
            {
                float fadeAlpha = fadeCurve.Evaluate(lifeRatio);

                float pulse = 1f;
                if (usePulseGlow && isDashing)
                {
                    pulse = 1f + Mathf.Sin(currentTime * pulseSpeed) * pulseIntensity;
                }

                // STRETCHING: Update end position to stretch with player movement
                if (isDashing)
                {
                    // Calculate how far the player has moved since this line spawned
                    float timeSinceSpawn = currentTime - line.spawnTime;
                    float distanceMoved = timeSinceSpawn * controller.Settings.dashSpeed; // You'll need to expose dash speed

                    // New end position = original start position + dash direction * (original length + distance moved)
                    Vector3 stretchedEndPos = line.startPos + (Vector3)dashDirection * (lineLength + distanceMoved);

                    line.coreRenderer.SetPosition(1, stretchedEndPos);
                    line.glowRenderer.SetPosition(1, stretchedEndPos);
                }

                // Update CORE renderer
                Color coreColor = lineColor;
                coreColor.a = lineColor.a * fadeAlpha * pulse;
                line.coreRenderer.startColor = coreColor;
                line.coreRenderer.endColor = coreColor;

                // Update GLOW renderer
                Color glow = glowColor;
                glow.a = glowColor.a * fadeAlpha * pulse * 0.8f;
                line.glowRenderer.startColor = glow;
                line.glowRenderer.endColor = glow;

                if (fadeWidth)
                {
                    float widthMultiplier = Mathf.Lerp(0.3f, 1f, fadeAlpha);
                    line.coreRenderer.startWidth = line.baseCoreWidth * widthMultiplier * pulse;
                    line.coreRenderer.endWidth = line.baseCoreWidth * widthMultiplier * pulse;
                    line.glowRenderer.startWidth = line.baseGlowWidth * widthMultiplier * pulse;
                    line.glowRenderer.endWidth = line.baseGlowWidth * widthMultiplier * pulse;
                }
            }
        }
    }

    private void SpawnLineSet()
    {
        if (activeLines.Count > 60) return;

        Vector2 perp = new Vector2(-dashDirection.y, dashDirection.x).normalized;

        Bounds bounds = new Bounds(transform.position, Vector3.one * 1.5f);
        if (playerSprite != null)
            bounds = playerSprite.bounds;
        else if (GetComponent<Collider2D>() != null)
            bounds = GetComponent<Collider2D>().bounds;

        float playerHeight = bounds.size.y;

        // Calculate the vertical range - from 1/8 below top to 1/8 above bottom
        float topBound = playerHeight * 0.5f; // Top of player from center
        float bottomBound = -playerHeight * 0.5f; // Bottom of player from center

        // Shrink the range by 1/8 from each end
        // 1/8 of total height = playerHeight * 0.125f
        float reducedTop = topBound - (playerHeight * 0.125f);
        float reducedBottom = bottomBound + (playerHeight * 0.125f);

        // Calculate center and new height for t calculation
        float centerY = (reducedTop + reducedBottom) * 0.5f;
        float reducedHeight = reducedTop - reducedBottom;

        for (int i = 0; i < lineCount; i++)
        {
            if (coreLinePool.Count == 0) CreateCoreLineRenderer();
            if (glowLinePool.Count == 0) CreateGlowLineRenderer();
            if (coreLinePool.Count == 0 || glowLinePool.Count == 0) continue;

            LineRenderer coreLine = coreLinePool.Dequeue();
            LineRenderer glowLine = glowLinePool.Dequeue();

            coreLine.gameObject.SetActive(true);
            glowLine.gameObject.SetActive(true);

            // Calculate offset for this line - now using reduced range
            float t;
            if (lineCount == 1)
            {
                t = 0f; // Center if only one line
            }
            else
            {
                t = (i / (float)(lineCount - 1)) * 2f - 1f; // -1 to 1
            }

            // Map t from -1..1 to reducedBottom..reducedTop
            float verticalOffset = Mathf.Lerp(reducedBottom, reducedTop, (t + 1f) * 0.5f);

            // Apply the offset using the perpendicular direction
            Vector3 offset = perp * verticalOffset;

            // Add slight random variation (reduced to maintain the bounds more strictly)
            offset += new Vector3(
                Random.Range(-0.02f, 0.02f),
                Random.Range(-0.02f, 0.02f),
                0
            );

            // CRITICAL FIX: Store the EXACT spawn positions
            // Start at current player position + offset
            Vector3 startPos = transform.position + offset;

            // End is exactly lineLength in dash direction from this start position
            Vector3 endPos = startPos + (Vector3)dashDirection * lineLength;

            // Set positions
            coreLine.SetPosition(0, startPos);
            coreLine.SetPosition(1, endPos);
            glowLine.SetPosition(0, startPos);
            glowLine.SetPosition(1, endPos);

            // Set colors and widths
            coreLine.startColor = lineColor;
            coreLine.endColor = lineColor;
            glowLine.startColor = glowColor;
            glowLine.endColor = glowColor;

            float baseCoreWidth = lineWidth * coreWidthMultiplier * (1f + Random.Range(-0.1f, 0.1f));
            float baseGlowWidth = lineWidth * glowWidthMultiplier * (1f + Random.Range(-0.1f, 0.1f));

            coreLine.startWidth = baseCoreWidth;
            coreLine.endWidth = baseCoreWidth;
            glowLine.startWidth = baseGlowWidth;
            glowLine.endWidth = baseGlowWidth;

            // Track with FIXED positions
            GlowingLine glowingLine = new GlowingLine
            {
                coreRenderer = coreLine,
                glowRenderer = glowLine,
                spawnTime = Time.time,
                startPos = startPos,
                endPos = endPos,
                baseCoreWidth = baseCoreWidth,
                baseGlowWidth = baseGlowWidth
            };

            activeLines.Add(glowingLine);
        }
    }

    private void ClearAllLines()
    {
        foreach (var line in activeLines)
        {
            if (line.coreRenderer != null)
            {
                line.coreRenderer.gameObject.SetActive(false);
                coreLinePool.Enqueue(line.coreRenderer);
            }
            if (line.glowRenderer != null)
            {
                line.glowRenderer.gameObject.SetActive(false);
                glowLinePool.Enqueue(line.glowRenderer);
            }
        }
        activeLines.Clear();
    }

    public void ForceCleanup()
    {
        ClearAllLines();
    }

    private void OnDisable()
    {
        ForceCleanup();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        if (isDashing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, dashDirection * 2f);

            Vector2 perp = new Vector2(-dashDirection.y, dashDirection.x).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, perp * lineSpread);
            Gizmos.DrawRay(transform.position, -perp * lineSpread);
        }
    }
}