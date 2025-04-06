using UnityEngine;

public class ImearBuilding : MonoBehaviour
{
    private SkillManager skillManager;
    private int totalProduction;

    // Start is called before the first frame update
    void Start()
    {
        AddResource(10);
    }

    private void AddResource(int _add)
    {
        totalProduction += _add;
        ResourceManager.Instance.AddImear(_add);
    }

}

