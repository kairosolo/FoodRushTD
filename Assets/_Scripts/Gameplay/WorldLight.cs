using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WorldLight : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Light2D light2D;
    [SerializeField] private SpriteRenderer lightVisual;

    [Header("Sprites")]
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private void Start()
    {
        if (WorldLightManager.Instance != null)
        {
            WorldLightManager.Instance.AddLight(this);
        }
        else
        {
            Debug.LogError("WorldLightManager instance not found! WorldLight cannot register itself.");
        }
    }

    private void OnDisable()
    {
        if (WorldLightManager.Instance != null)
        {
            WorldLightManager.Instance.RemoveLight(this);
        }
    }

    public void TurnOn()
    {
        if (light2D != null) light2D.enabled = true;
        if (lightVisual != null) lightVisual.sprite = onSprite;
    }

    public void TurnOff()
    {
        if (light2D != null) light2D.enabled = false;
        if (lightVisual != null) lightVisual.sprite = offSprite;
    }
}