// DashLineTrail.cs
using System.Collections.Generic;
using UnityEngine;

public class KalbDashLineTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Line Settings - THIN LINES")]
    [SerializeField] private int lineCount = 8;
    [SerializeField] private float lineLength = 3f;
    [SerializeField] private float lineWidth = 0.05f; // MUCH thinner
    [SerializeField] private float lineSpread = 0.6f;
    [SerializeField] private Material lineMaterial;

    [Header("Colors")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 1f); // Bright white

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private class SimpleLine
    {
        public LineRenderer renderer;
        public float spawnTime;
        public Vector3 startPos;
        public Vector3 endPos;
    }

    private List<SimpleLine> activeLines = new List<SimpleLine>();
    private Queue<LineRenderer> linePool = new Queue<LineRenderer>();
    private bool isDashing = false;
    private Vector2 dashDirection;
    private float dashStartTime;
    private float dashDuration;
    private Transform lineContainer;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<KalbController>();
        if (playerSprite == null) playerSprite = GetComponent<SpriteRenderer>();

        // Create container
        GameObject container = new GameObject("DashLineContainer");
        lineContainer = container.transform;
        lineContainer.SetParent(transform);
        lineContainer.localPosition = Vector3.zero;

        // Create initial pool
        for (int i = 0; i < 30; i++)
        {
            CreateLineRenderer();
        }


    }

    private LineRenderer CreateLineRenderer()
    {
        GameObject lineObj = new GameObject("DashLine");
        lineObj.transform.SetParent(lineContainer);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;

        // Material
        if (lineMaterial != null)
            line.material = lineMaterial;
        else
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.color = Color.white;
        }

        // CRITICAL: Constant width - no tapering
        line.startWidth = lineWidth;
        line.endWidth = lineWidth; // SAME as start width - no taper!

        // Color - start with full alpha
        line.startColor = lineColor;
        line.endColor = lineColor; // SAME color - we'll fade both ends together

        // Rendering settings
        line.sortingOrder = 1000;
        line.sortingLayerName = "Default";
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // No width curve (keeps constant width)
        //line.widthCurve = null;
        line.widthMultiplier = 1f;

        // Deactivate and add to pool
        lineObj.SetActive(false);
        linePool.Enqueue(line);

        return line;
    }

    public void StartDashLines(Vector2 direction, float duration)
    {


        isDashing = true;
        dashDirection = direction.normalized;
        dashDuration = duration;
        dashStartTime = Time.time;

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

        // Spawn new lines while dashing (every frame)
        if (isDashing)
        {
            SpawnLineSet();
        }



        // Update existing lines (just fade, no shrinking)
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            SimpleLine line = activeLines[i];

            if (line.renderer == null)
            {
                activeLines.RemoveAt(i);
                continue;
            }

            float age = currentTime - line.spawnTime;
            float lifeRatio = age / dashDuration;

            if (lifeRatio >= 1f)
            {
                // Line expired
                line.renderer.gameObject.SetActive(false);
                linePool.Enqueue(line.renderer);
                activeLines.RemoveAt(i);
            }
            else
            {
                // ONLY fade alpha - keep same width
                float alpha = 1f - lifeRatio;
                Color fadedColor = lineColor;
                fadedColor.a = lineColor.a * alpha;

                // Apply same faded color to both ends
                line.renderer.startColor = fadedColor;
                line.renderer.endColor = fadedColor;

                // Keep positions updated if we want them to stretch with player
                if (isDashing)
                {
                    // Update end position to current player position + direction
                    Vector3 currentEndPos = transform.position + (Vector3)dashDirection * lineLength;
                    line.renderer.SetPosition(1, currentEndPos);
                }
            }
        }
    }

    private void SpawnLineSet()
    {
        if (activeLines.Count > 60) return; // Limit total lines

        // Calculate perpendicular direction
        Vector2 perp = new Vector2(-dashDirection.y, dashDirection.x).normalized;

        // Get player bounds
        Bounds bounds = new Bounds(transform.position, Vector3.one * 1.5f);
        if (playerSprite != null)
            bounds = playerSprite.bounds;
        else if (GetComponent<Collider2D>() != null)
            bounds = GetComponent<Collider2D>().bounds;

        float playerHeight = bounds.size.y;

        for (int i = 0; i < lineCount; i++)
        {
            if (linePool.Count == 0)
            {
                CreateLineRenderer();
            }

            if (linePool.Count == 0) continue;

            // Get line from pool
            LineRenderer line = linePool.Dequeue();
            line.gameObject.SetActive(true);

            // Calculate offset for this line
            float t = (i / (float)(lineCount - 1)) * 2f - 1f; // -1 to 1
            float offsetAmount = t * lineSpread * (playerHeight * 0.5f);

            Vector3 offset = perp * offsetAmount;

            // Add slight random variation for natural look
            offset += new Vector3(
                Random.Range(-0.03f, 0.03f),
                Random.Range(-0.03f, 0.03f),
                0
            );

            // Calculate line positions
            Vector3 startPos = transform.position + offset;

            // End position is along dash direction
            Vector3 endPos = startPos + (Vector3)dashDirection * lineLength;

            // Set line positions
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);

            // Set constant color (will fade in update)
            line.startColor = lineColor;
            line.endColor = lineColor;

            // Ensure constant width
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            // Track this line
            SimpleLine simpleLine = new SimpleLine
            {
                renderer = line,
                spawnTime = Time.time,
                startPos = startPos,
                endPos = endPos
            };

            activeLines.Add(simpleLine);
        }
    }

    private void ClearAllLines()
    {
        foreach (var line in activeLines)
        {
            if (line.renderer != null)
            {
                line.renderer.gameObject.SetActive(false);
                linePool.Enqueue(line.renderer);
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