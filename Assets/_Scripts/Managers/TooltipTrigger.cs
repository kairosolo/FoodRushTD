using UnityEngine;

public class TooltipTrigger : MonoBehaviour
{
    [SerializeField] private string title;

    [TextArea(3, 10)]
    [SerializeField] private string description;

    public void OnPointerEnter()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(title, description);
        }
    }

    public void OnPointerExit()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    public void SetText(string newTitle, string newDescription)
    {
        this.title = newTitle;
        this.description = newDescription;
    }
}