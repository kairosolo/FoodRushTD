using UnityEngine;
using TMPro;

public class FastForwardUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private string textFormat = ">> x{0:F1}";

    private const float DEFAULT_TIME_SCALE = 1f;

    private void OnEnable()
    {
        TimeManager.OnTimeScaleChanged += HandleTimeScaleChanged;

        if (TimeManager.Instance != null)
        {
            HandleTimeScaleChanged(TimeManager.Instance.CurrentTimeScale);
        }
    }

    private void OnDisable()
    {
        TimeManager.OnTimeScaleChanged -= HandleTimeScaleChanged;
    }

    private void HandleTimeScaleChanged(float newTimeScale)
    {
        if (newTimeScale > DEFAULT_TIME_SCALE + 0.01f)
        {
            Show(newTimeScale);
        }
        else
        {
            Hide();
        }
    }

    private void Show(float currentSpeed)
    {
        if (container != null) container.SetActive(true);
        if (speedText != null) speedText.text = string.Format(textFormat, currentSpeed);
    }

    private void Hide()
    {
        if (container != null) container.SetActive(false);
    }
}