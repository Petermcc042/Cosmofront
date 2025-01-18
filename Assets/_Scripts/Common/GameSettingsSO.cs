using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GameSettingsSO : ScriptableObject
{
    public int numberOfSpawns;
    public int gridWidth;
    public int gridLength;

    public int totalTime;

    [SerializeField] public List<GameObject> enemyPrefabList;
    public List<int> enemyWeightList;
    public List<int> enemyTypeList;
    public List<int> enemyXPList;
    public List<int> enemyHealthList;
    public List<int> enemyArmourList;
    public List<float> spawnIntervalList;
    public List<int> phaseLengthList;
}
