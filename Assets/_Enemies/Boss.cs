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
    }

    // Update is called once per frame
    private void Update()
    {
        if (pathVectorList.Count > 0) { UpdateEnemyPosition(); }
        
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
