using UnityEngine;


public class ResourceData : MonoBehaviour
{
#pragma warning disable UDR0001 // Domain Reload Analyzer
    private static ResourceData instance;
#pragma warning restore UDR0001 // Domain Reload Analyzer

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