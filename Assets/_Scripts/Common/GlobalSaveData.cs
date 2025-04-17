using UnityEngine;


public class GlobalSaveData : MonoBehaviour
{
#pragma warning disable UDR0001 // Domain Reload Analyzer
    private static GlobalSaveData instance;
#pragma warning restore UDR0001 // Domain Reload Analyzer

    public bool refreshData;

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

        SaveSystem.LoadGame();

        if(refreshData)
        {
            SaveSystem.InitData();
            SaveSystem.SaveGame();
        }

        SaveSystem.LoadGame();
    }


}