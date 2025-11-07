using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class DynamicSorterGroup : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float sortingPrecision = 100f;

    private SortingGroup sortingGroup;
    private Transform rootTransform;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        rootTransform = transform.root;
    }

    private void LateUpdate()
    {
        if (sortingGroup != null && rootTransform != null)
        {
            sortingGroup.sortingOrder = -(int)(rootTransform.position.y * sortingPrecision);
        }
    }
}