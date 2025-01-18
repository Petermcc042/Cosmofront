using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[BurstCompile]
public struct ReturnTargetIndex : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyArray;
    public NativeArray<float> EnemyDistanceArray;
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
    Explosive, Piercing, ChainLightning, Spread, Slow, Ricochet, Overcharge,
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
    public float range = 5f;
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
    private float meteorCountdown = 5f;
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

    private Transform target;

    [Header("Stats Attributes")]
    public int turretXP;
    public int xpToAdd;
    public int turretLevel;
    public UpgradeType upgradeType;
    public List<BulletType> bulletTypes;

    [Header("Unity Setup")]
    public Transform partToRotate;
    public Transform firePoint;
    [SerializeField] public GameObject highlightBox;

    public bool autoUpgrade;

    private void Awake()
    {
        skillManager = GameObject.Find("SkillManager").GetComponent<SkillManager>();
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        collisionManager = GameObject.Find("CollisionManager").GetComponent<CollisionManager>();
        turretManager = GameObject.Find("TurretManager").GetComponent<TurretManager>();

        skillManager.OnSkillUnlocked += SkillManager_OnSkillUnlocked;
        collisionManager.TurretXPEvent += OnTurretXPEvent;

        autoUpgrade = gameManager.autoUpgrade;
        bulletTypes.Add(BulletType.Standard);
        bulletTypes.Add(BulletType.Blank);
    }

    private void OnDestroy()
    {
        skillManager.OnSkillUnlocked -= SkillManager_OnSkillUnlocked;
        collisionManager.TurretXPEvent -= OnTurretXPEvent;
    }

    private void Update()
    {
        if (gameManager.GetGameState()) return;

        SequentialUpdate();
    }

    private void SequentialUpdate()
    {
        HandleXPAddition();
        if (bulletTypes.Contains(BulletType.Overcharge))
        {
            overchargeMultiplier += Time.deltaTime;
        }

        UpdateTargeting();

        if (orbitalAnimating)
        {
            AnimateOrbitalStrike();
            return;
        }

        if (target == null) return;

        UpdateRotation();
        HandleShooting();
        HandleLargeUpgrades();

    }

    private void HandleXPAddition()
    {
        if (xpToAdd <= 0) { return; }

        turretXP++;
        xpToAdd--;

        TurretUpgrade turretUpgrade = new() { TurretRef = this, UpgradeTypeRef = UpgradeType.Small, Position = 0 };

        // Check upgrade milestones
        switch (turretXP)
        {
            case 20:
            case 40:
            case 60:
            case 100:
            case 120:
            case 160:
            case 180:
                turretUpgrade.UpgradeTypeRef = UpgradeType.Small;
                turretManager.AddTurretForUpgrade(turretUpgrade);
                break;
            case 80:
                turretUpgrade.UpgradeTypeRef = UpgradeType.Medium;
                turretManager.AddTurretForUpgrade(turretUpgrade);
                break;
            case 140:
                turretUpgrade.UpgradeTypeRef = UpgradeType.Medium;
                turretUpgrade.Position = 1;
                turretManager.AddTurretForUpgrade(turretUpgrade);
                break;
            case 200:
                turretUpgrade.UpgradeTypeRef = UpgradeType.Large;
                turretManager.AddTurretForUpgrade(turretUpgrade);
                break;
        }
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
        BulletManager.Instance.SpawnBullet(firePoint.position, _dir.normalized, 40, gameObject.GetInstanceID(), Mathf.RoundToInt(bulletDamage * (1 + overchargeMultiplier)), passThrough, bulletTypes[0], bulletTypes[1]);
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
        BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80, gameObject.GetInstanceID(), 40, 0, BulletType.OrbitalStrike, BulletType.Blank);
        Destroy(orbitalLaser);
        orbitalAnimating = false;
    }

    private void CallMeteorShower()
    {
        if (meteorCountdown <= 0f)
        {
            Transform meteorTarget = UpdateTarget(meteorSearchRadius);
            if (meteorTarget != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 spawnPos = meteorTarget.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));
                    BulletManager.Instance.SpawnBullet(spawnPos, Vector3.down, 80, gameObject.GetInstanceID(), 40, 0, BulletType.MeteorShower, BulletType.Blank);
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
                BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80, gameObject.GetInstanceID(), 5, 0, BulletType.FirestormPayload, BulletType.Blank);
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
                BulletManager.Instance.SpawnBullet(spawnPos, Vector3.zero, 80, gameObject.GetInstanceID(), 5, 0, BulletType.TimewarpPayload, BulletType.Blank);
            }

            timewarpCountdown = timewarpFireDelay;
        }
        timewarpCountdown -= Time.deltaTime;
    }

    private Transform UpdateTarget(float _range)
    {
        float shortestDistance = _range + 5f;
        GameObject nearestEnemy = null;

        NativeArray<EnemyData> tempEnemyArray = collisionManager.ReturnEnemyDataList().AsArray();
        NativeArray<float> tempEnemyDistanceArray = new(tempEnemyArray.Length, Allocator.Persistent);

        var job = new ReturnTargetIndex
        {
            EnemyArray = tempEnemyArray,
            EnemyDistanceArray = tempEnemyDistanceArray,
            CurrentTransform = transform.position
        };

        JobHandle handle = job.Schedule(tempEnemyArray.Length, 64);
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

        tempEnemyArray.Dispose();
        tempEnemyDistanceArray.Dispose();

        return nearestEnemy != null && shortestDistance <= range ? nearestEnemy.transform : null;
    }

    private void OnTurretXPEvent(object sender, CollisionManager.TurretXPEventArgs e)
    {
        if (e.turretID != gameObject.GetInstanceID()) return;

        killCount++;
        xpToAdd += e.xpAmount;
        //turretManager.AddTurretForUpgrade(this);
    }

    private void SkillManager_OnSkillUnlocked(object sender, SkillManager.OnSkillUnlockedEventArgs e)
    {
        if (!e.global)
        {
            if (e.buildingID != gameObject.GetInstanceID()) { return; }
        }

        switch (e.skillType)
        {
            case SkillManager.SkillType.ExplosiveRound:
                bulletTypes[1] = BulletType.Explosive;
                fireRate = 1;
                passThrough = 0;
                targetingRate = 0.1f;
                break;
            case SkillManager.SkillType.LightningRound:
                bulletTypes[1] = BulletType.ChainLightning;
                fireRate = 1;
                passThrough = 0;
                targetingRate = 0.1f;
                break;
            case SkillManager.SkillType.SlowRound:
                bulletTypes[1] = BulletType.Slow;
                passThrough = 6;
                targetingRate = 0.1f;
                break;
            case SkillManager.SkillType.SpreadRound:
                bulletTypes[1] = BulletType.Spread;
                passThrough = 0;
                fireRate = 3;
                targetingRate = 0.1f;
                break;
            case SkillManager.SkillType.RicochetRound:
                bulletTypes[1] = BulletType.Ricochet;
                fireRate = 25;
                targetingRate = 0.1f;
                break;
            case SkillManager.SkillType.OrbitalStrike:
                allowOrbitalStrike = true;
                break;
            case SkillManager.SkillType.MeteorShower:
                allowMeteorShower = true;
                break;
            case SkillManager.SkillType.Overclocked:
                bulletDamage = 1;
                fireRate = 10;
                targetingRate = 0.1f;
                passThrough = 5;
                break;
            case SkillManager.SkillType.Firestorm:
                allowFirestorm = true;
                break;
            case SkillManager.SkillType.Timewarp:
                allowTimewarp = true;
                break;
        }
    }
}
