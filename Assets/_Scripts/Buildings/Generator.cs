using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


public class Generator : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    //[SerializeField] MapGridManager mapGridManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private BuildableAreaMesh shieldAreaMesh;
    [SerializeField] private GameObject circumPoint;
    [SerializeField] private bool showCircleCirum;
    [SerializeField] GameObject shield;
    private List<float3> shieldSquares;

    [SerializeField] private Slider shieldHealthUI;

    private float health = 10;
    private float startingHealth = 10;
    private int shieldRadius = 5; // The radius of the shield in world units
    public float gridSize = 1; // The size of each grid square in world units

    private bool isRecharging = false;

    private float lastDamageTime;
    private const float rechargeDelay = 2f; // 2 seconds delay
    private const float rechargeRate = 3f; // Rate at which the shield recharges

    public NativeList<float3> shieldGridSquareList;


    private void Awake()
    {
        shield.transform.localScale = new float3(50, 50, 50);

        shieldSquares = GetCirclePoints(100, 100, shieldRadius, 0.2f);
        shieldGridSquareList = new NativeList<float3>(Allocator.Persistent);

        // keep for testing
        //shieldAreaMesh.buildableAreas = shieldSquares;
        //shieldAreaMesh.GenerateMesh();
    }

    private void OnDestroy()
    {
        shieldGridSquareList.Dispose();
    }

    public void UpdateShieldHealth(int _increase)
    {
        health += _increase;
        startingHealth = health;
    }


    public void CheckShieldSquares(bool _toRemove)
    {
        shieldGridSquareList.Clear();

        if (_toRemove) { return; }

        for (int i = 0; i < shieldSquares.Count; i++)
        {
            if (showCircleCirum)
            {
                Instantiate(circumPoint, shieldSquares[i], Quaternion.identity);
            }
            shieldGridSquareList.Add(shieldSquares[i]);
        }

    }

    public void DamageLoop(float _damage, float _deltaTime)
    {
        shieldHealthUI.value = health / startingHealth;

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

    // Function to get positions on the circumference of a circle with a specified distance between each point
    public List<float3> GetCirclePoints(float centerX, float centerZ, float radius, float distanceBetweenPoints)
    {
        List<float3> points = new List<float3>();

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
            points.Add(new float3(x, 0, z));
        }

        return points;
    }

    public static void RemoveFromNativeList(NativeList<float3> list, float3 positionToRemove)
    {
        // Find and remove the position using a swap-and-pop approach
        for (int i = list.Length - 1; i >= 0; i--)
        {
            if (list[i].Equals(positionToRemove))
            {
                list.RemoveAtSwapBack(i);
                break; // Exit once we've found and removed the position
            }
        }
    }

    public void UpdateShieldSize(int radius)
    {
        int transRadius = radius * 10;
        shield.transform.localScale = new float3(transRadius, transRadius, transRadius);

        shieldSquares = GetCirclePoints(100, 100, radius, 0.2f);
        CheckShieldSquares(false);
    }
}
