using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemyPathfinding : MonoBehaviour
{
    public List<Vector3> pathVectorList;
    private int currentPathIndex = 0;
    private const float speed = 3f;


    public void SendPath(List<Vector3> _pathVectorList)
    {
        pathVectorList = _pathVectorList;
        //currentPathIndex = 0;
        //Debug.Log("Path list received of length " + pathVectorList.Count);
    }

    private void Update()
    {
        //Debug.Log(pathVectorList != null);
        HandleMovement();
    }

    private void HandleMovement()
    {

        if (pathVectorList != null) {
            Vector3 targetPosition = pathVectorList[currentPathIndex];

            if (Vector3.Distance(transform.position, targetPosition) > 1f)
            {
                Vector3 moveDir = (targetPosition - transform.position).normalized;

                //float distanceBefore = Vector3.Distance(transform.position, targetPosition);
                transform.position = transform.position + moveDir * speed * Time.deltaTime;
            } else
            {
                currentPathIndex++;
                if (currentPathIndex >= pathVectorList.Count)
                {
                    StopMoving();
                    Destroy(gameObject);
                }
            }
        }
    }

    private void StopMoving() {
        pathVectorList = null;
    }


}
