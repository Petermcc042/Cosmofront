using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using System;
using Unity.Mathematics;

public struct BulletData
{
    public int TurretID;
    public float3 Position;
    public float3 Velocity;
    public float Lifetime;
    public int Speed;
    public int Damage;
    public bool ToRemove;
    public int PassThrough;
    public BulletType Type;
    public FixedList128Bytes<int> hitEnemies; 
    // Fixed-length lists (specify element count)
    // FixedList<T, N>        // Where N is the capacity
}



public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance { get; private set; }

    [SerializeField] public EnemyManager enemyManager;
    [SerializeField] public CollisionManager collisionManager;

    [SerializeField] private GameObject standardBulletPrefab;
    [SerializeField] private GameObject explosiveBulletPrefab; 
    [SerializeField] private GameObject spreadBulletPrefab;
    [SerializeField] private GameObject orbitalStrikePrefab;
    [SerializeField] private GameObject meteorShowerPrefab;
    [SerializeField] private GameObject firestormPrefab;

    private List<GameObject> bulletObjects;
    public NativeList<BulletData> bulletDataList;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Set the instance to this object
        Instance = this;

        // Initialize bullets
        bulletObjects = new List<GameObject>();
        bulletDataList = new NativeList<BulletData>(Allocator.Persistent);
    }

    public void SpawnBullet(Vector3 _origin, Vector3 _direction, int _speed, int _turretID, int _damage, int _passThrough, BulletType _bulletType)
    {
        GameObject bullet = Instantiate(GetBulletPrefab(_bulletType), _origin, Quaternion.identity, gameObject.transform);
        bulletObjects.Add(bullet);

        BulletData bulletData = new()
        {
            Position = _origin,
            Velocity = _direction,
            Lifetime = 0,
            Speed = _speed,
            Damage = _damage,
            PassThrough = _passThrough,
            TurretID = _turretID,
            Type = _bulletType
        };

        bulletDataList.Add(bulletData);
    }

    private GameObject GetBulletPrefab(BulletType bulletType)
    {
        switch (bulletType)
        {
            case BulletType.Explosive:
                return explosiveBulletPrefab;
            case BulletType.OrbitalStrike:
                return orbitalStrikePrefab;
            case BulletType.MeteorShower:
                return meteorShowerPrefab;
            case BulletType.Spread:
                return spreadBulletPrefab;
            case BulletType.Firestorm:
                return firestormPrefab;
            case BulletType.Timewarp:
                return firestormPrefab;
            default:
                return standardBulletPrefab;
        }
    }

    public void UpdateBulletData(NativeList<int> _toRemove, NativeList<BulletData> _bulletDataList)
    {
        for (int i = 0; i < _toRemove.Length; i++)
        {
            Destroy(bulletObjects[_toRemove[i]]);
            bulletObjects.RemoveAt(_toRemove[i]);
        }

        for (int i = 0; i < bulletObjects.Count; i++)
        {
            bulletObjects[i].transform.position = _bulletDataList[i].Position;
        }
    }

    [BurstCompile]
    public struct AddListItemsJob : IJob
    {
        public NativeList<BulletData> BulletsToAdd;
        public NativeList<BulletData> BulletList;

        public void Execute()
        {
            for (int i = 0; i < BulletsToAdd.Length; i++)
            {
                BulletList.Add(BulletsToAdd[i]);
            }
            BulletsToAdd.Clear();
        }
    }

}





