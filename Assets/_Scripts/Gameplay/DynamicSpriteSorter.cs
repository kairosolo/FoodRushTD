using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class DynamicSpriteSorter : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float sortingPrecision = 100f;

    private SortingGroup sortingGroup;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
    }

    private void LateUpdate()
    {
        sortingGroup.sortingOrder = -(int)(transform.position.y * sortingPrecision);
    }
}