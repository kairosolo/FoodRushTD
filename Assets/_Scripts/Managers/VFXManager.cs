using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [SerializeField] private GameObject projectileExplodeVFX;
    [SerializeField] private GameObject customerHappyVFX;
    [SerializeField] private GameObject stationPlacementVFX;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayVFX(string effectName, Vector3 position, Quaternion rotation)
    {
        GameObject vfx = null;
        switch (effectName)
        {
            case "projectileExplodeVFX":
                vfx = Instantiate(projectileExplodeVFX, position, rotation);
                break;
            case "customerHappyVFX":
                vfx = Instantiate(customerHappyVFX, position, rotation);
                break;
            case "stationPlacementVFX":
                vfx = Instantiate(stationPlacementVFX, position, rotation);
                break;
        }

        if (vfx != null)
            Destroy(vfx, 2f);
    }


}