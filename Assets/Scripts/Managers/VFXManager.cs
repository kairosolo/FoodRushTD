using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [SerializeField] private GameObject alertVFXPrefab;


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
            case "AlertVFX":
                vfx = Instantiate(alertVFXPrefab, position, rotation);
                break;
        }

        if (vfx != null)
            Destroy(vfx, 2f);
    }


}