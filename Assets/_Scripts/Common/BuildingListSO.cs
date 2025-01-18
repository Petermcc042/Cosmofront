using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BuildingListSO : ScriptableObject
{
    [Header("Generic Buildings")]
    [SerializeField] public PlacedObjectSO habitatLightSO;
    [SerializeField] public PlacedObjectSO WallSO;
    [SerializeField] public PlacedObjectSO TurretSO;
    [SerializeField] public PlacedObjectSO MG_SO;
    [SerializeField] public PlacedObjectSO Canon_SO;
    [SerializeField] public PlacedObjectSO marcumSO;
    [SerializeField] public PlacedObjectSO attaniumSO;
    [SerializeField] public PlacedObjectSO imearSO;
    [SerializeField] public PlacedObjectSO ScannerSO;
    [SerializeField] public PlacedObjectSO LabSO;
    [SerializeField] public PlacedObjectSO builderBrigadeSO;
    [SerializeField] public PlacedObjectSO minersGuildSO;
    [SerializeField] public PlacedObjectSO generatorSO;

    [Header("Environment PlacedObjectSO")]
    [SerializeField] public PlacedObjectSO rockSO;
    [SerializeField] public PlacedObjectSO smallRockSO;
}
