using UnityEngine;
using System.Collections.Generic;

public class StationSelectionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject stationButtonPrefab;
    [SerializeField] private Transform buttonContainer;

    private void OnEnable() => ProgressionManager.OnAvailableStationsChanged += UpdateStationButtons;

    private void OnDisable() => ProgressionManager.OnAvailableStationsChanged -= UpdateStationButtons;

    private void Start()
    {
        UpdateStationButtons();
    }

    public void SetInteractable(bool isInteractable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = isInteractable;
        }
    }

    private void UpdateStationButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        List<StationData> availableStations = ProgressionManager.Instance.AvailableStations;
        foreach (var stationData in availableStations)
        {
            GameObject buttonObject = Instantiate(stationButtonPrefab, buttonContainer);
            if (buttonObject.TryGetComponent<StationButton>(out StationButton stationButton))
            {
                stationButton.Initialize(stationData);
            }
        }
    }
}