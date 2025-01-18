using UnityEngine;

public class EnemyWalker : MonoBehaviour
{
    private EnemyManager enemyManager;

    private void Awake()
    {
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
    }
}
