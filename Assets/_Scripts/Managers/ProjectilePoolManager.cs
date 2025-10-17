using UnityEngine;
using System.Collections.Generic;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [SerializeField] private Transform poolContainer;

    private Dictionary<GameObject, Queue<GameObject>> projectilePools = new Dictionary<GameObject, Queue<GameObject>>();

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
    }

    public GameObject GetProjectile(GameObject projectilePrefab)
    {
        if (!projectilePools.ContainsKey(projectilePrefab))
        {
            projectilePools.Add(projectilePrefab, new Queue<GameObject>());
        }

        Queue<GameObject> pool = projectilePools[projectilePrefab];

        if (pool.Count > 0)
        {
            GameObject projectile = pool.Dequeue();
            projectile.SetActive(true);
            return projectile;
        }
        else
        {
            GameObject newProjectile = Instantiate(projectilePrefab, poolContainer);

            PooledProjectile pooledComponent = newProjectile.GetComponent<PooledProjectile>();
            if (pooledComponent == null)
            {
                Debug.LogError($"Projectile prefab '{projectilePrefab.name}' is missing the PooledProjectile component!");
                return newProjectile;
            }

            pooledComponent.OriginalPrefab = projectilePrefab;
            return newProjectile;
        }
    }

    public void ReturnProjectile(GameObject projectileInstance)
    {
        PooledProjectile pooledComponent = projectileInstance.GetComponent<PooledProjectile>();

        if (pooledComponent == null || pooledComponent.OriginalPrefab == null)
        {
            Debug.LogWarning("Trying to return a projectile that doesn't have a PooledProjectile component or original prefab. Destroying it instead.");
            Destroy(projectileInstance);
            return;
        }

        GameObject originalPrefab = pooledComponent.OriginalPrefab;

        if (projectilePools.ContainsKey(originalPrefab))
        {
            projectileInstance.SetActive(false);
            projectilePools[originalPrefab].Enqueue(projectileInstance);
        }
        else
        {
            Debug.LogWarning($"Trying to return projectile to a pool that doesn't exist for prefab '{originalPrefab.name}'. Destroying it instead.");
            Destroy(projectileInstance);
        }
    }
}