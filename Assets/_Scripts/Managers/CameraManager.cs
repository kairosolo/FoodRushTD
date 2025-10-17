using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public CameraShake MainCameraShake { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        if (Camera.main != null)
        {
            MainCameraShake = Camera.main.GetComponent<CameraShake>();
            if (MainCameraShake == null)
            {
                Debug.LogWarning("CameraShake component not found on the main camera. Adding it automatically.");
                MainCameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
            }
        }
        else
        {
            Debug.LogError("Main Camera not found in the scene!");
        }
    }
}