using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttaniumBuilding : MonoBehaviour
{
    private SkillManager skillManager;
    [SerializeField] private int totalProduction;

    // Start is called before the first frame update
    void Start()
    {
        AddResource(5);
    }

    private void AddResource(int _add)
    {
        totalProduction += _add;
        ResourceManager.Instance.AddAttanium(_add);
    }

    private void AddMarcum(int _add)
    {
        totalProduction += _add;
        ResourceManager.Instance.AddMarcum(_add);
    }
}
