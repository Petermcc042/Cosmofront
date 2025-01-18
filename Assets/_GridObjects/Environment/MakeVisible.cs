using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeVisible : MonoBehaviour
{
    [SerializeField] GameObject visibleGO;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "HabitatLight")
            visibleGO.SetActive(true);
    }
}
