using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public GameObject particlePrefab;
    public int poolSize = 10;
    private Queue<GameObject> particlePool;

    void Start()
    {
        particlePool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject particleObject = Instantiate(particlePrefab);
            particleObject.SetActive(false);
            particlePool.Enqueue(particleObject);
        }
    }

    public void SpawnParticle(Vector3 position)
    {
        if (particlePool.Count > 0)
        {
            GameObject particle = particlePool.Dequeue();
            particle.transform.position = position;
            particle.SetActive(true);
            StartCoroutine(ReturnToPool(particle, 2f));  // Recycle after 2 seconds
        }
    }

    private IEnumerator ReturnToPool(GameObject particle, float delay)
    {
        yield return new WaitForSeconds(delay);
        particle.SetActive(false);
        particlePool.Enqueue(particle);
    }
}