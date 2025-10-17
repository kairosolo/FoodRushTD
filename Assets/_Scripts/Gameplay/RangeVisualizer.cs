using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [Range(10, 100)]
    [SerializeField] private int segments = 50;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color color = new Color(0, 1, 1, 0.5f);

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
    }

    public void Show(float radius)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        CreatePoints(radius);
    }

    public void Hide()
    {
        lineRenderer.positionCount = 0;
    }

    private void CreatePoints(float radius)
    {
        lineRenderer.positionCount = segments + 1;

        float angle = 0f;
        float angleStep = 360f / segments;

        for (int i = 0; i < (segments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));

            angle += angleStep;
        }
    }
}