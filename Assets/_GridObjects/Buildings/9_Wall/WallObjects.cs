using System;
using System.Collections.Generic;
using UnityEngine;

public class WallObjects : MonoBehaviour
{
    private WallManager wm;
    private SkillManager skillManager;
    private EnemyManager enemyManager;

    [SerializeField] private Renderer objectRenderer;
    private Color originalColor;


    private List<int>enemyList;
    private List<GameObject>enemyObjects;

    private int health = 1;

    [SerializeField] private List<Vector2Int> gridPosList;

    private void Awake()
    {
        skillManager = GameObject.Find("SkillManager").GetComponent<SkillManager>();
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
        wm = GameObject.Find("WallManager").GetComponent<WallManager>();

        enemyList = new List<int>();
        enemyObjects = new List<GameObject>();

        originalColor = objectRenderer.material.color;

        enemyManager.EnemyDamaged += WO_EnemyDamaged;
        enemyManager.EnemyDestroyed += WO_EnemyDestroyed;
        skillManager.OnSkillUnlocked += SkillManager_OnSkillUnlocked;

        MapGridManager.Instance.mapGrid.GetXZ(gameObject.transform.position, out int x, out int z);
    }

    private void WO_EnemyDamaged(object sender, EnemyManager.EnemyDamageEventArgs e)
    {
        if (!enemyList.Contains(e.enemyObjectID))
        {
            // If not, exit the method
            return;
        }
        DamageLoop(e.damagedAmount);
    }

    private void WO_EnemyDestroyed(object sender, EnemyManager.EnemyDestroyedEventArgs e)
    {
        if (!enemyList.Contains(e.enemyObjectID))
        {
            // If not, exit the method
            return;
        }
        enemyList.Remove(e.enemyObjectID);
    }

    private void SkillManager_OnSkillUnlocked(object sender, SkillManager.OnSkillUnlockedEventArgs e)
    {
        switch (e.skillType)
        {
            case SkillManager.SkillType.WallStrength:
                RepairWallsHealth();
                break;
            case SkillManager.SkillType.PathworkPoly:
                RepairWallsHealth();
                break;
        }
    }

    private void OnDestroy()
    {
        enemyManager.EnemyDamaged -= WO_EnemyDamaged;
        enemyManager.EnemyDestroyed -= WO_EnemyDestroyed;
        skillManager.OnSkillUnlocked -= SkillManager_OnSkillUnlocked;
    }

    public void UpdateKillCount()
    {
        return;
    }

    private void RepairWallsHealth()
    {
        Debug.Log("Walls Healed");
        health += 10;
        objectRenderer.material.color = originalColor;
    }


    
    private void OnTriggerEnter(Collider other)
    {
        GameObject enemy = other.gameObject;

        if (enemy.GetComponent<EnemyBaseClass>() != null)
        {
            wm.NewEnemyContact(enemy);
            enemyList.Add(enemy.GetInstanceID());
            enemyObjects.Add(enemy);
        }
    }

    public void DamageLoop(int _damageAmount)
    {
        health -= _damageAmount;

        if (health > 0)
        {
            Color tempColor = objectRenderer.material.color;
            objectRenderer.material.color = new Color(tempColor.r + 0.1f, tempColor.g, tempColor.b);
        }
        else
        {
            DisableWall();
        }

    }

    private void DisableWall()
    {
        //MapGridManager.Instance.DestroyBuilding(transform.position);
    }
}
