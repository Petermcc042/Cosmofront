using UnityEngine;

public class MarcumBuilding : MonoBehaviour
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
        ResourceManager.Instance.AddMarcum(_add);
    }

    private void AddAttResource(int _add)
    {
        //totalProduction += _add;
        ResourceManager.Instance.AddAttanium(_add);
    }

    private void AddImearResource(int _add)
    {
        //totalProduction += _add;
        ResourceManager.Instance.AddImear(_add);
    }

}

