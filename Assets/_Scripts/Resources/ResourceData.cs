using UnityEngine;


public class ResourceData : MonoBehaviour
{
    private static ResourceData instance;

    public int totalAttanium;
    public int totalMarcum;
    public int totalImear;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CommitSessionResources(int _att, int _marc, int _imear)
    {
        totalAttanium += _att;
        totalMarcum += _marc;
        totalImear += _imear;
    }

    public void DecrementResources(int _att, int _marc, int _imear)
    {
        totalAttanium -= _att;
        totalMarcum -= _marc;
        totalImear -= _imear;
    }
}