using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct ReturnTargetIndex : IJobParallelFor
{
    public NativeArray<float> EnemyDistanceArray;

    [ReadOnly] public NativeList<EnemyData> EnemyArray;
    [ReadOnly] public Vector3 CurrentTransform;

    public void Execute(int index)
    {
        EnemyDistanceArray[index] = Vector3.Distance(EnemyArray[index].Position, CurrentTransform);
    }
}

public enum BulletType
{
    Blank,
    Standard,
    Explosive, Piercing, 
    Spread, Circler, SpreadCircles,
    Slow, Ricochet, Overcharge,
    ChainLightning, ArcLightning, PlasmaOverload,
    OrbitalStrike, MeteorShower, Overclocked, FirestormPayload, Firestorm, TimewarpPayload, Timewarp
}

public enum UpgradeType { Small, Medium, Large }

public class Turret : MonoBehaviour
{
    private SkillManager skillManager;
    private GameManager gameManager;
    private EnemyManager enemyManager;
    private CollisionManager collisionManager;
    private TurretManager turretManager;

    [Header("Damage Attributes")]
    public float fireRate = 1f; // higher is faster
    public int killCount = 0;
    public int bulletDamage = 1;
    public int fleshMultiplier = 1;
    public int armourMultiplier = 1;
    public int critChance = 0;
    public int critMultiplier = 2;
    public int passThrough = 0; // zero means it hits 1 enemy

    private float overchargeMultiplier;

    [Header("Targeting Attributes")]
    public float range = 3f;
    public float turnSpeed = 10f;
    public float fireCountdown = 0f;
    public float targetingRate = 1f;
    public float targetingCountdown = 0f;
    public float lockOnSpeed = 1f;

    [Header("Orbital Strike Attributes")]
    public float orbitalSearchRadius = 20f;
    public float orbitalDamageRadius = 5f;
    public float orbitalFireDelay = 0f;
    private float orbitalCountdown = 5f;
    [SerializeField] private GameObject orbitalRadiusGO;
    public bool allowOrbitalStrike = false;

    private bool orbitalAnimating = false;
    private GameObject orbitalLaser;
    private Material orbitalLaserMaterial;
    private float orbitalAnimationTimer;

    [Header("Meteor Shower Attributes")]
    public float meteorSearchRadius = 20f;
    public float meteorDamageRadius = 5f;
    public float meteorFireDelay = 0f;
    private float meteorCountdown = 2f;
    [SerializeField] private GameObject meteorRadiusGO;
    public bool allowMeteorShower = false;

    [Header("Firestorm Attributes")]
    public float firestormSearchRadius = 20f;
    public float firestormDamageRadius = 5f;
    public float firestormFireDelay = 0f;
    private float firestormCountdown = 5f;
    [SerializeField] private GameObject firestormRadiusGO;
    public bool allowFirestorm = false;

    [Header("Timewarp Attributes")]
    public float timewarpSearchRadius = 20f;
    public float timewarpDamageRadius = 5f;
    public float timewarpFireDelay = 5f;
    private float timewarpCountdown = 5f;
    [SerializeField] private GameObject timewarpRadiusGO;
    public bool allowTimewarp = false;

    

    [Header("Stats Attributes")]
    public int turretID;
    public int turretXP;
    public int xpToAdd;
    public int turretLevel;
    public UpgradeType upgradeType;
    public BulletType bulletType;
    public int fireRateUpgrades = 0;
    public int targetingRateUpgrades  = 0;
    public int targetingRangeUpgrades = 0;
    public int damageUpgrades = 0;

    [Header("Unity Setup")]
    public Transform partToRotate;
    public Transform firePoint;
    [SerializeField] public GameObject highlightBox;

    [Header("Upgrades")]
    public List<IUpgradeOption> unlockedUpgradeList;

    private Transform target;
    private NativeList<EnemyData> enemyDataList;


    private void Awake()
    {
        skillManager = GameObject.Find("SkillManager").GetComponent<SkillManager>();
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        collisionManager = GameObject.Find("CollisionManager").GetComponent<CollisionManager>();
        turretManager = GameObject.Find("TurretManager").GetComponent<TurretManager>();


        turretID = gameObject.GetInstanceID();
        turretManager.AddTurret(this);

        unlockedUpgradeList = new List<IUpgradeOption>();
        enemyDataList = enemyManager.enemyDataList;
    }

    private void OnDestroy()
    {
        turretManager.RemoveTurret(this);

    }

    public void CallUpdate()
    {
        if (bulletType == BulletType.Overcharge)
        {
            overchargeMultiplier += Time.deltaTime;
        }

        UpdateTargeting();

        if (orbitalAnimating)
        {
            AnimateOrbitalStrike();
        }

        if (target == null) return;

        UpdateRotation();
        HandleShooting();
        HandleLargeUpgrades();
    }

    private void UpdateTargeting()
    {
        if (targetingCountdown <= 0f)
        {
            target = UpdateTarget(range);
            targetingCountdown = 1f / fireRate;
        }
        targetingCountdown -= Time.deltaTime;
    }

    private void UpdateRotation()
    {
        Vector3 direction = target.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
        partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
    }

    private void HandleShooting()
    {
        if (fireCountdown <= 0f)
        {
            firePoint.LookAt(target.position);
            Shoot(target.position - transform.position);
            fireCountdown = 1f / fireRate;
            overchargeMultiplier = 0;
        }
        fireCountdown -= Time.deltaTime;
    }

    private void Shoot(Vector3 _dir)
    {
        BulletManager.Instance.SpawnBullet(firePoint.position, _dir.normalized, 40, turretID, Mathf.RoundToInt(bulletDamage * (1 + overchargeMultiplier)), passThrough, bulletType);
    }

    public bool HasUpgrade(IUpgradeOption upgradeType)
    {
        return unlockedUpgradeList.Contains(upgradeType);
    }

    private void HandleLargeUpgrades()
    {
        if (allowOrbitalStrike)
        {
            CallOrbitalStrike();
        }
        if (allowMeteorShower)
        {
            CallMeteorShower();
        }
        if (allowFirestorm)
        {
            CallFirestorm();
        }
        if (allowTimewarp)
        {
            CallTimewarp();
        }
    }

    private void CallOrbitalStrike()
    {
        if (orbitalCountdown <= 0f)
        {
            Transform orbitalTarget = UpdateTarget(orbitalSearchRadius);
            if (orbitalTarget != null)
            {
                StartOrbitalAnimation(orbitalTarget.position);
            }

            orbitalCountdown = orbitalFireDelay;
        }
        orbitalCountdown -= Time.deltaTime;
    }

    private void StartOrbitalAnimation(Vector3 targetPosition)
    {
        orbitalAnimating = true;
        orbitalAnimationTimer = 0f;

        orbitalLaser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        orbitalLaser.transform.position = targetPosition + new Vector3(0, 40f, 0);
        orbitalLaser.transform.localScale = new Vector3(0.1f, 40f, 0.1f);

        orbitalLaserMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        orbitalLaserMaterial.SetColor("_BaseColor", new Color(1f, 0f, 0f, 1f));
        orbitalLaserMaterial.EnableKeyword("_EMISSION");
        orbitalLaserMaterial.SetColor("_EmissionColor", Color.red * 0.5f);

        Renderer renderer = orbitalLaser.GetComponent<Renderer>();
        renderer.material = orbitalLaserMaterial;
    }

    private void AnimateOrbitalStrike()
    {
        orbitalAnimationTimer += Time.deltaTime;

        if (orbitalAnimationTimer <= 1f)
        {
            float intensity = Mathf.Lerp(0.5f, 2f, orbitalAnimationTimer / 1f);
            orbitalLaserMaterial.SetColor("_EmissionColor", Color.red * intensity);
        }
        else if (orbitalAnimationTimer <= 3f)
        {
            float expandTime = orbitalAnimationTimer - 1f;
            Vector3 initialScale = new Vector3(0.1f, 40f, 0.1f);
            Vector3 targetScale = new Vector3(1f, 40f, 1f);
            orbitalLaser.transform.localScale = Vector3.Lerp(initialScale, targetScale, expandTime / 2f);

            float alpha = Mathf.Lerp(1f, 0f, expandTime / 2f);
            Color baseColor = orbitalLaserMaterial.GetColor("_BaseColor");
            orbitalLaserMaterial.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
        }
        else
        {
            EndOrbitalAnimation();
        }
    }

    private void EndOrbitalAnimation()
    {
        Vector3 spawnPos = orbitalLaser.transform.position - new Vector3(0, 40f, 0);
        BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80,turretID, 40, 0, BulletType.OrbitalStrike);
        Destroy(orbitalLaser);
        orbitalAnimating = false;
    }

    private void CallMeteorShower()
    {
        if (meteorCountdown <= 0f)
        {
            Debug.Log("Calling shower");
            Transform meteorTarget = UpdateTarget(meteorSearchRadius);
            if (meteorTarget != null)
            {
                float meteorHeight = 100f; // Height above the target to spawn meteors

                for (int i = 0; i < 3; i++)
                {
                    // Calculate random offset for target position
                    Vector3 targetPos = meteorTarget.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

                    // Calculate spawn position above the target
                    Vector3 spawnPos = new Vector3(targetPos.x, targetPos.y + meteorHeight, targetPos.z);

                    // Spawn the meteor at the elevated position, targeting down toward the target position
                    Vector3 direction = (targetPos - spawnPos).normalized;

                    BulletManager.Instance.SpawnBullet(spawnPos, direction, 70, turretID, 40, 0, BulletType.MeteorShower);
                }
            }
            meteorCountdown = meteorFireDelay;
        }
        meteorCountdown -= Time.deltaTime;
    }

    private void CallFirestorm()
    {
        if (firestormCountdown <= 0f)
        {
            Transform firestormTarget = UpdateTarget(firestormSearchRadius);
            if (firestormTarget != null)
            {
                Vector3 spawnPos = firestormTarget.position;
                BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80, turretID, 5, 0, BulletType.FirestormPayload);
            }

            firestormCountdown = firestormFireDelay;
        }
        firestormCountdown -= Time.deltaTime;
    }

    private void CallTimewarp()
    {
        if (timewarpCountdown <= 0f)
        {
            Transform timewarpTarget = UpdateTarget(timewarpSearchRadius);
            if (timewarpTarget != null)
            {
                Vector3 spawnPos = timewarpTarget.position;
                BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80, turretID, 5, 0, BulletType.TimewarpPayload);
            }

            timewarpCountdown = timewarpFireDelay;
        }
        timewarpCountdown -= Time.deltaTime;
    }

    private Transform UpdateTarget(float _range)
    {
        float shortestDistance = _range + 5f;
        GameObject nearestEnemy = null;

        NativeArray<float> tempEnemyDistanceArray = new(enemyDataList.Length, Allocator.Persistent);

        var job = new ReturnTargetIndex
        {
            EnemyArray = enemyDataList,
            EnemyDistanceArray = tempEnemyDistanceArray,
            CurrentTransform = transform.position
        };

        JobHandle handle = job.Schedule(enemyDataList.Length, 64);
        handle.Complete();

        for (int i = 0; i < tempEnemyDistanceArray.Length; i++)
        {
            float distanceToEnemy = tempEnemyDistanceArray[i];
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemyManager.ReturnEnemyObjectList()[i];
            }
        }

        tempEnemyDistanceArray.Dispose();

        return nearestEnemy != null && shortestDistance <= range ? nearestEnemy.transform : null;
    }
}
