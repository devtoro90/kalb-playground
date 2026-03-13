using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private float scaleSpeed = 1f;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            textMesh = gameObject.AddComponent<TextMeshPro>();

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 24;
        textMesh.color = Color.white;
    }

    public void SetDamage(int damage)
    {
        textMesh.text = damage.ToString();

        // Randomize initial position slightly
        transform.position += new Vector3(
            Random.Range(-0.2f, 0.2f),
            Random.Range(0f, 0.3f),
            0
        );

        StartCoroutine(AnimateNumber());
    }

    private System.Collections.IEnumerator AnimateNumber()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * 2f;

        float timer = 0f;
        float duration = 1f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // Move upward
            transform.position = Vector3.Lerp(startPosition, endPosition, t);

            // Fade out
            Color color = textMesh.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            textMesh.color = color;

            // Scale up
            transform.localScale = Vector3.one * (1f + t * 0.5f);

            yield return null;
        }

        Destroy(gameObject);
    }
}