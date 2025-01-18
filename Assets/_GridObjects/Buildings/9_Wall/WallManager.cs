using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    public List<Vector3> wallList;
    public List<GameObject> wallGOList;

    private void Awake()
    {
        wallList = new List<Vector3>();
    }

    public void AddWall(Vector3 _wallCoord, GameObject _wallObject)
    {
        wallList.Add(_wallCoord);
        wallGOList.Add(_wallObject);
    }

    public event EventHandler<EnemyTouchedEventArgs> EnemyTouched;
    public class EnemyTouchedEventArgs : EventArgs { public GameObject enemyObject; }

    public void NewEnemyContact(GameObject _instance)
    {
        EnemyTouched?.Invoke(this, new EnemyTouchedEventArgs { enemyObject = _instance });
    }
}
