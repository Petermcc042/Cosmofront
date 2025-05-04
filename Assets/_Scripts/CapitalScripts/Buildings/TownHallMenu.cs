using UnityEngine;

public class TownHallMenu : MonoBehaviour
{
    public void IncreasePopulation() //ui reference
    {
        SaveSystem.playerData.populationTotal += 100;
    }
}
