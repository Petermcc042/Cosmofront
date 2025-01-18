using UnityEngine;

public class BuildersBrigadeBuilding : MonoBehaviour
{
    private SkillManager skillManager;
    private EnemyManager enemyManager;

    private void Awake()
    {
        skillManager = GameObject.Find("SkillManager").GetComponent<SkillManager>();
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
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
    }

}
