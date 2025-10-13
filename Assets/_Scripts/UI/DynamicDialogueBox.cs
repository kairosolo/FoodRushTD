using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class DynamicDialogueBox : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The container holding the order icons (must have a LayoutGroup).")]
    [SerializeField] private RectTransform contentContainer;

    [Header("Sizing")]
    [Tooltip("Extra space added to the sides of the content.")]
    [SerializeField] private float horizontalPadding = 20f;

    [Tooltip("The minimum width of the dialogue box.")]
    [SerializeField] private float minWidth = 80f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // When this UI is enabled, start the resizing process
        StartCoroutine(ResizeRoutine());
    }

    private IEnumerator ResizeRoutine()
    {
        // Yielding null waits for the next frame. Yielding WaitForEndOfFrame
        // is even better as it waits until all rendering for the frame is complete.
        yield return new WaitForEndOfFrame();

        // After the LayoutGroup has calculated the size of its children...
        if (contentContainer != null)
        {
            float contentWidth = LayoutUtility.GetPreferredWidth(contentContainer);
            float newWidth = Mathf.Max(minWidth, contentWidth + horizontalPadding);

            // Adjust our own size
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }
    }
}