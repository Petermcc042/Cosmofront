using System;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    public PlacedObjectSO placedObjectSO;
    public Vector2Int origin;
    public PlacedObjectSO.Dir dir;
    [SerializeField] private GameObject damageVisual;

    public string visibleName;
    public int upgradeLevel = 0;
    public int upgradePath = 0;
    public float health = 10;


    public static PlacedObject Create(Vector3 worldPosition, Vector2Int origin, PlacedObjectSO.Dir dir, PlacedObjectSO placedObjectSO)
    {
        GameObject parentGameObjects = GameObject.Find(placedObjectSO.parentNameString);
        Transform placedObjectTransform = Instantiate(placedObjectSO.startPrefab, worldPosition, Quaternion.Euler(0, placedObjectSO.GetRotationAngle(dir), 0));
        placedObjectTransform.transform.parent = parentGameObjects.transform;

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();

        placedObject.placedObjectSO = placedObjectSO;
        placedObject.origin = origin;
        placedObject.dir = dir;
        placedObject.visibleName = placedObjectSO.nameString;

        return placedObject;
    }

    public void ActivateDamageVisual()
    {
        damageVisual.SetActive(true);
    }

    public void DealDamage(float _damage)
    {
        health -= _damage;
    }

    public PlacedObjectSO ReturnSO()
    {
        return placedObjectSO;
    }

    public List<Vector2Int> GetGridPositionList()
    {
        return placedObjectSO.GetGridPositionList(origin, dir);
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

}
