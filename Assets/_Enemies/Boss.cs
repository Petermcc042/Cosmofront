using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour, IEnemy
{
    [SerializeField]  private readonly bool showDebugLines;

    private List<Vector3> pathVectorList;
    private const float speed = 15f;
    
    public int currentIndexPosition;

    public GameObject parentGameObject;

    private int health = 15;

    public void SetParentObject(GameObject _parentGameObject)
    {
        parentGameObject = _parentGameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        Transform centre = GameObject.Find("centre").transform;
        pathVectorList = SetTargetPosition(centre.position, new List<Vector3>());
    }

    // Update is called once per frame
    private void Update()
    {
        if (pathVectorList.Count > 0) { UpdateEnemyPosition(); }
        
    }

    private List<Vector3> SetTargetPosition(Vector3 targetPosition, List<Vector3> _existingPath)
    {
        List<Vector3> pathList = Pathfinding.Instance.FindPath(transform.position, targetPosition);

        if (showDebugLines)
        {
            if (pathList != null)
            {
                for (int i = 0; i < pathList.Count - 1; i++)
                {
                    //Debug.Log(path[i].x + ":" + path[i].z + " -> " + path[i+1].x + ":" + path[i+1].z);
                    Debug.DrawLine(new Vector3(pathList[i].x, 1, pathList[i].z) + Vector3.one * 0.5f, new Vector3(pathList[i + 1].x, 1, pathList[i + 1].z) + Vector3.one * 0.5f, UnityEngine.Color.black, 100f);
                }
            }
        }

        if (pathList != null && pathList.Count > 1)
        {
            //pathList.RemoveAt(0);
        }

        // start spawning now that we have the path
        return pathList;
    }



    private void UpdateEnemyPosition()
    {         
        Vector3 targetPosition = pathVectorList[currentIndexPosition];

        if (Vector3.Distance(transform.position, targetPosition) > 1f)
        {
            Vector3 moveDir = (targetPosition - transform.position).normalized;

            //float distanceBefore = Vector3.Distance(transform.position, targetPosition);
            transform.position = transform.position + moveDir * speed * Time.deltaTime;
        }
        else
        {
            IncrementIndex();

            if (currentIndexPosition >= pathVectorList.Count)
            {
                parentGameObject.GetComponent<GameManager>().EndGame("You Lost");
                DestroyEnemy(gameObject);
            }
        }
        
    }

    public void IncrementIndex() { currentIndexPosition += 1; }

    public void DestroyEnemy(GameObject _enemy)
    {
        Destroy(_enemy);
    }

    public void Damage(GameObject _gameObject, int damage, int _criticalDamage, int _armourDamage)
    {
        health -= 1;
        if (health <= 0)
        {
            parentGameObject.GetComponent<GameManager>().EndGame("You Won");
            DestroyEnemy(gameObject);
        }
    }

    public void Touched()
    {
        throw new System.NotImplementedException();
    }
}
