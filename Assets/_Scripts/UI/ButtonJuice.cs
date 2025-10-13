using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Squash and Stretch Settings")]
    [SerializeField] private float squashAmount = 0.85f;
    [SerializeField] private float stretchAmount = 1.15f;
    [SerializeField] private float duration = 0.2f;

    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Coroutine _scaleRoutine;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _originalScale = _rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScale(new Vector3(squashAmount, stretchAmount, 1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScale(_originalScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartScale(_originalScale);
    }

    private void StartScale(Vector3 target)
    {
        if (_rectTransform == null || !gameObject.activeInHierarchy) return;
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleRoutine(target));
    }

    private IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = _rectTransform.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease-out
            _rectTransform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        _rectTransform.localScale = target;
    }

    private void OnDisable()
    {
        if (_rectTransform != null)
            _rectTransform.localScale = _originalScale;

        if (_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);
    }
}
