using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Generator : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private BuildableAreaMesh shieldAreaMesh;
    [SerializeField] private GameObject circumPoint;
    [SerializeField] private bool showCircleCirum;
    [SerializeField] GameObject shield;
    private List<Vector3> shieldSquares;

    [SerializeField] private Slider shieldHealthUI;

    public event EventHandler<MapGridManager.BuildingAddedEventArgs> BuildingAddedEvent;

    private float health = 10;
    public int shieldRadius = 5; // The radius of the shield in world units
    public float gridSize = 1; // The size of each grid square in world units

    private bool isRecharging = false;

    private float lastDamageTime;
    private const float rechargeDelay = 2f; // 2 seconds delay
    private const float rechargeRate = 3f; // Rate at which the shield recharges


    private void Awake()
    {
        shield.transform.localScale = new Vector3(50, 50, 50);

        shieldSquares = GetCirclePoints(100, 100, shieldRadius, 0.2f);
        //shieldAreaMesh.buildableAreas = shieldSquares;
        //shieldAreaMesh.GenerateMesh();
    }


    public void CheckShieldSquares(bool _toRemove)
    {
        for (int i = 0; i < shieldSquares.Count; i++)
        {
            if (showCircleCirum)
            {
                Instantiate(circumPoint, shieldSquares[i], Quaternion.identity);
            }

            BuildingAddedEvent?.Invoke(this, new MapGridManager.BuildingAddedEventArgs { coord = shieldSquares[i], remove = _toRemove, shield = true });
        }

    }

    public void DamageLoop(float _damage, float _deltaTime)
    {
        shieldHealthUI.value = health / 10;

        // If the shield takes damage, reset the recharge timer
        if (_damage > 0)
        {
            lastDamageTime = Time.time;
            float damageAmount = _damage;
            health -= damageAmount;

            if (health <= 0)
            {
                isRecharging = true;
                shield.SetActive(false);
                CheckShieldSquares(true);
            }
        }
        else if (Time.time - lastDamageTime >= rechargeDelay && isRecharging)
        {
            // Start recharging if enough time has passed since last damage
            health += rechargeRate * _deltaTime;

            if (health >= 10) // Assuming 10 is the maximum shield health
            {
                health = 10;
                isRecharging = false;
                shield.SetActive(true);
                CheckShieldSquares(false);
            }
        }
    }

    List<Vector3> CalculateShieldGridSquares()
    {
        List<Vector3> shieldGridSquares = new List<Vector3>();

        int centerX = 200 / 2;
        int centerZ = 200 / 2;

        for (int i = -shieldRadius; i <= shieldRadius; i++)
        {
            for (int j = -shieldRadius; j <= shieldRadius; j++)
            {
                int distanceSquared = i * i + j * j;
                int outerRadiusSquared = shieldRadius * shieldRadius;
                int innerRadiusSquared = (shieldRadius - 1) * (shieldRadius - 1);

                // Check if the point (i, j) is on the boundary of the circle
                if (distanceSquared <= outerRadiusSquared && distanceSquared > innerRadiusSquared)
                {
                    int gridPosX = centerX + i;
                    int gridPosZ = centerZ + j;

                    shieldGridSquares.Add(new Vector3(gridPosX, 0, gridPosZ));
                }
            }
        }

        return shieldGridSquares;
    }

    // Function to get positions on the circumference of a circle with a specified distance between each point
    public List<Vector3> GetCirclePoints(float centerX, float centerZ, float radius, float distanceBetweenPoints)
    {
        List<Vector3> points = new List<Vector3>();

        // Calculate the circumference of the circle
        float circumference = 2 * Mathf.PI * radius;

        // Calculate the required number of points based on the desired distance between each point
        int numberOfPoints = Mathf.CeilToInt(circumference / distanceBetweenPoints);

        // Calculate the angle between each point in radians
        float angleStep = 2 * Mathf.PI / numberOfPoints;

        // Loop through and calculate each point on the circumference
        for (int i = 0; i < numberOfPoints; i++)
        {
            float angle = i * angleStep;

            // Calculate the x and z coordinates of the point on the circumference
            float x = centerX + Mathf.Cos(angle) * radius;
            float z = centerZ + Mathf.Sin(angle) * radius;

            // Add the point to the list (keeping y as 0 for flat on ground or adjust as needed)
            points.Add(new Vector3(x, 0, z));
        }

        return points;
    }
}
