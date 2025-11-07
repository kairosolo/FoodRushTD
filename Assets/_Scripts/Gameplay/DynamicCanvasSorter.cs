using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class DynamicCanvasSorter : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float sortingPrecision = 100f;

    private Canvas canvas;
    private Transform rootTransform;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        rootTransform = transform.root;
    }

    private void LateUpdate()
    {
        if (canvas != null && rootTransform != null)
        {
            canvas.sortingOrder = -(int)(rootTransform.position.y * sortingPrecision);
        }
    }
}