using UnityEngine;
using System.Collections;

public class CashUIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float scaleAmount = 1.25f;
    [SerializeField] private float animationDuration = 0.2f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine runningCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        EconomyManager.OnCashCollected += HandleCashCollected;
    }

    private void OnDisable()
    {
        EconomyManager.OnCashCollected -= HandleCashCollected;
    }

    private void HandleCashCollected()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(AnimateCashRoutine());
    }

    private IEnumerator AnimateCashRoutine()
    {
        float timer = 0f;
        Vector3 targetScale = originalScale * scaleAmount;

        while (timer < animationDuration / 2)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / (animationDuration / 2);
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        timer = 0f;

        while (timer < animationDuration / 2)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / (animationDuration / 2);
            rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }
}