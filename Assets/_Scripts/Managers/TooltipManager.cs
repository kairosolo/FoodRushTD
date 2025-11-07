using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] private GameObject tooltipContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (tooltipContainer != null)
        {
            tooltipContainer.SetActive(false);
        }
    }

    private void Update()
    {
    }

    public void ShowTooltip(string title, string description)
    {
        if (tooltipContainer == null) return;

        titleText.text = title;
        descriptionText.text = description;
        tooltipContainer.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipContainer == null) return;

        tooltipContainer.SetActive(false);
    }
}