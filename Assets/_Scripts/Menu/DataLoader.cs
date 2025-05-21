using UnityEngine;

public class DataLoader : MonoBehaviour
{
    [SerializeField] private bool freshData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SaveSystem.LoadGame();

        if (freshData )
        {
            SaveSystem.InitData();
        }
    }
}
