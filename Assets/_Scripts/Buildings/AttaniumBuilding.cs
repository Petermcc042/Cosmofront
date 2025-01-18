using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttaniumBuilding : MonoBehaviour
{
    private SkillManager skillManager;
    [SerializeField] private int totalProduction;

    private void Awake()
    {
        skillManager = GameObject.Find("SkillManager").GetComponent<SkillManager>();
        skillManager.OnSkillUnlocked += SkillManager_OnSkillUnlocked;
    }

    private void OnDestroy()
    {
        skillManager.OnSkillUnlocked -= SkillManager_OnSkillUnlocked;
    }

    private void SkillManager_OnSkillUnlocked(object sender, SkillManager.OnSkillUnlockedEventArgs e)
    {
        if (e.buildingID != gameObject.GetInstanceID())
        {
            // If not, exit the method
            return;
        }
        switch (e.skillType)
        {
            case SkillManager.SkillType.BiggerDrill:
                AddResource(Mathf.RoundToInt(totalProduction*0.1f));
                break;
            case SkillManager.SkillType.MorePower:
                AddResource(Mathf.RoundToInt(totalProduction * 0.2f));
                break;
            case SkillManager.SkillType.ExtractionEfficiency:
                AddResource(totalProduction);
                break;
            case SkillManager.SkillType.ImprovedScanner:
                AddResource(Mathf.RoundToInt(totalProduction * 0.2f));
                break;
            case SkillManager.SkillType.ExtraTooling:
                AddMarcum(5);
                break;
            case SkillManager.SkillType.DoubleDrill:
                AddMarcum(5);
                AddResource(totalProduction);
                break;
        }
    }

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
