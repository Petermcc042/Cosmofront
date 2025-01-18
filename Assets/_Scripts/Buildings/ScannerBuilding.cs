using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ScannerBuilding : MonoBehaviour
{

    private EnemyManager enemyManager;
    private SkillManager skillManager;

    private void Awake()
    {
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
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
            case SkillManager.SkillType.AdvanceIntel:
                Debug.Log(e.skillType);
                break;
            case SkillManager.SkillType.LongRangeScan:
                Debug.Log(e.skillType);
                break;
            case SkillManager.SkillType.OmegaThreat:
                Debug.Log(e.skillType);
                break;
        }
    }
}
