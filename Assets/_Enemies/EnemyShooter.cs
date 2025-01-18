using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

public class EnemyShooter : MonoBehaviour
{
    private EnemyManager enemyManager;

    // For the shooting
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private void Awake()
    {
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
    }

    public void StartShooting()
    {
        //GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        //projectile.transform.parent = gameObject.transform;
        // Apply force in the forward direction
        //projectile.GetComponent<Rigidbody>().velocity = firePoint.forward * 60;

    }
}
