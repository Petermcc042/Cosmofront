using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Dev Controls")]
    [SerializeField] private bool costsRemoved;
    [SerializeField] public bool autoUpgrade;
    [SerializeField] public bool invincible;

    [Header("Level Settings")]
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private BuildingListSO buildingsList;

    [Header("Manager References")]
    [SerializeField] private MapGridManager gridManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private PhaseManager phaseManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private NewPathfinding pathfinder;
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private TurretManager turretManager;


    [Header("Building Aid")]
    [SerializeField] private GameObject buildingUI;
    [SerializeField] private GameObject extendedArea;
    [SerializeField] private GameObject buildableArea;


    [SerializeField] private GameObject tempObjectShowing;
    private Vector3 tempBuildingOffset;

    public LayerMask layerMask;
    [SerializeField] LayerMask buildingLayerMask;

    private PlacedObjectSO currentSO;

    [Header("Timer Management")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI phaseTMP;
    private float totalTime;
    [SerializeField] private List<int> phaseIntervals;



    [SerializeField] private AudioClip musicClip;
    private AudioSource music;


    // Total time for the countdown
    private float remainingTime; // Time left

    [Header("Enemy Manager")]
    [SerializeField] private GameObject boss1;
   

    [Header("End Menu UI")]
    [SerializeField] private GameObject endMenuUI;
    [SerializeField] private TextMeshProUGUI endTextUI;
    [SerializeField] private GameObject skillMenuUI;

    [Header("Building Menu UI")]
    [SerializeField] private TextMeshProUGUI activeBuildingText;
    [SerializeField] private TextMeshProUGUI attActiveCostText;
    [SerializeField] private TextMeshProUGUI marcumActiveCostText;
    [SerializeField] private TextMeshProUGUI imearActiveCostText;

    [Header("Upgrades Menu UI")]
    [SerializeField] private GameObject rightPanelUI;
    [SerializeField] private TextMeshProUGUI turretNameUI;
    [SerializeField] private TextMeshProUGUI bulletDamageUI;
    [SerializeField] private TextMeshProUGUI killCountUI;
    [SerializeField] private TextMeshProUGUI fireRateUI;
    [SerializeField] private TextMeshProUGUI turretLevelUI;


    [Header("Escape Menu UI")]
    [SerializeField] private GameObject escapeMenuUI;
    private bool gameRunning = true;

    // help for menu ui
    private GameObject lastSelectedBuilding;
    private int lastSelectedPath;
    private Vector3 lastSelectedCost;

    private SaveData playerData;


    private void Awake()
    {
        gameRunning = false;
        Time.timeScale = 1;
        totalTime = gameSettings.totalTime;

        CountAllCSLinesInScriptsFolder();

        playerData = saveSystem.LoadGame();

        if (playerData != null)
        {
            Debug.Log("Loaded Data");
        }
        else
        {
            SaveData saveData = new SaveData
            {
                playerLevel = 0,
                generatorHealthIncrease = 0f,
                playerPosition = new Vector3(1, 2, 3),
                inventoryItems = new List<string> { "Sword", "Shield" }
            };
            saveSystem.SaveGame(saveData);

            playerData = saveSystem.LoadGame();
        }
    }

    void Start()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.clip = musicClip;
        music.Play();
        
        // Begin all other script processes
        MapGridManager.Instance.InitGrid(gameSettings, buildingsList);
        collisionManager.InitCollisionManager(gameSettings);
        enemyManager.InitEnemyManager(gameSettings);
        phaseManager.InitPhaseManager(gameSettings);

        remainingTime = totalTime;

        currentSO = buildingsList.WallSO;

        resourceManager.AddAttanium(1);
        resourceManager.SetAttanium(50);

        gameRunning = true;
    }


    private void Update()
    {
        HandleInput();
        UpdateBuildingUI();
        UpdateRotation();

        if (!gameRunning) { return; }

        resourceManager.CallUpdate();
        phaseManager.CallUpdate();

        turretManager.CallUpdateAllTurrets();
        turretManager.CallUpdate();
        
        collisionManager.CallUpdate();
        enemyManager.CallUpdate();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.P)) { InvertGameState(); }

        if (Input.GetKeyDown(KeyCode.B)) { ToggleBuilding(); }

        if (Input.GetKeyDown(KeyCode.Escape)) { PauseGame(); }

        if (Input.GetKeyDown(KeyCode.Q)) { skillMenuUI.SetActive(!skillMenuUI.activeSelf); }

        if (Input.GetKeyDown(KeyCode.E)) { skillManager.IsSkillUnlocked(SkillManager.SkillType.TurretFireRate); }

        if (Input.GetKeyDown(KeyCode.J)) { enemyManager.RecalcPaths(); }
    }

    private void UpdateBuildingUI()
    {
        if (!buildingUI.activeSelf)
        {
            HandleBuildingUIInactive();
            return;
        }

        MapGridManager.Instance.mapGrid.GetXZ(GetMouseWorldPosition(), out int x, out int z);
        Vector3 gridPos = new Vector3(x, 0, z);

        if (MapGridManager.Instance.InBuildableArea(gridPos))
        {
            tempObjectShowing.SetActive(true);
            tempObjectShowing.transform.position = new Vector3(x, 0, z) + tempBuildingOffset;

            if (currentSO.nameString == "Habitat Light")
            {
                PlaceHabitatLight(x, z);
            }
            else
            {
                HandleGeneralBuildingPlacement(x, z);
            }
        }
        else
        {
            tempObjectShowing.SetActive(false);
        }
    }

    private void HandleBuildingUIInactive()
    {
        extendedArea.SetActive(false);
        tempObjectShowing.SetActive(false);

        if (Input.GetMouseButtonDown(0) && !IsOverUI())
        {
            CheckBuildingClick();
        }
    }

    private void HandleGeneralBuildingPlacement(int x, int z)
    {
        extendedArea.SetActive(false);

        if (!IsOverUI() && Input.GetMouseButtonDown(0) && CheckCost(currentSO.buildingCost))
        {
            MapGridManager.Instance.PlaceBuilding(GetMouseWorldPosition(), currentSO);
        }

        if (Input.GetMouseButtonDown(1))
        {
            MapGridManager.Instance.mapGrid.GetXZ(GetMouseWorldPosition(), out int _x, out int _z);
            MapGridManager.Instance.DestroyBuilding(_x,_z);

        }
    }

    private void UpdateRotation()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;

        var mapGridManager = MapGridManager.Instance;

        mapGridManager.RotateBuilding();
        tempObjectShowing.SetActive(true);

        mapGridManager.mapGrid.GetXZ(GetMouseWorldPosition(), out int x, out int z);
        PlacedObjectSO.Dir direction = mapGridManager.GetPlacedBuildingDirection();

        tempObjectShowing.transform.rotation = Quaternion.Euler(
            tempObjectShowing.transform.rotation.eulerAngles.x,
            currentSO.GetRotationAngle(direction),
            tempObjectShowing.transform.rotation.eulerAngles.z
        );

        Vector2Int tempOffset = currentSO.GetRotationOffset(direction);
        tempBuildingOffset = new Vector3(tempOffset.x, 0, tempOffset.y);
    }

    private void PauseGame()
    {
        if (gameRunning == true)
        {
            Time.timeScale = 0;
            escapeMenuUI.SetActive(true);
            gameRunning = false;
        } 
        else
        {
            Time.timeScale = 1;
            escapeMenuUI.SetActive(false);
            gameRunning = true;
        }

    }

    private void PlaceHabitatLight(int _x, int _z)
    {
        extendedArea.SetActive(true);
        extendedArea.transform.position = new Vector3(_x + 0.5f, 0, _z + 0.5f);

        if (!IsOverUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                Collider[] colliders = Physics.OverlapSphere(new Vector3(_x, 0, _z), 9, layerMask);

                // check current building resource cost against how much we have
                if (colliders.Length < 1 && ResourceManager.Instance.GetAttanium() >= currentSO.buildingCost.x)
                {
                    MapGridManager.Instance.PlaceBuilding(GetMouseWorldPosition(), currentSO);
                    ResourceManager.Instance.SetAttanium(-(int)currentSO.buildingCost.x);
                }
            }
        }
    }

    private bool CheckCost(Vector3 _costVector)
    {
        if (costsRemoved) { return true; }

        if (ResourceManager.Instance.GetAttanium() >= _costVector.x && ResourceManager.Instance.GetMalcan() >= _costVector.y && ResourceManager.Instance.GetImear() >= _costVector.z)
        {
            ResourceManager.Instance.SubtractResources((int)_costVector.x, (int)_costVector.y, (int)_costVector.z);
            return true;
        }
        
        return false;
    }

    public void ToggleBuilding()
    {
        rightPanelUI.SetActive(false);

        // toggle for building UI on screen
        buildingUI.SetActive(!buildingUI.activeSelf);
        buildableArea.SetActive(!buildableArea.activeSelf);
    }

    public void CloseRightPanel()
    {
        rightPanelUI.SetActive(false);
        Time.timeScale = 1;
    }

    private void CheckBuildingClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, buildingLayerMask))
        {
            Turret _turret = raycastHit.collider.GetComponent<Turret>();
            rightPanelUI.gameObject.SetActive(true);

            turretNameUI.text = $"{_turret.GetInstanceID()}";
            bulletDamageUI.text = $"Bullet Damage: {_turret.bulletDamage}";
            fireRateUI.text = $"Fire Rate: {_turret.fireRate}";
            killCountUI.text = $"Kill Count: {_turret.killCount}";
            turretLevelUI.text = $"Level: {_turret.turretLevel}";
        }
        else
        {
            // remove the last active building reference so it won't get updated next time
            if (!IsOverRightPanel())
            {
                rightPanelUI.gameObject.SetActive(false);
            }
        }
    }


    public void TryUpgradeBuilding()
    {
        if (CheckCost(lastSelectedCost))
        {
            skillManager.UnlockSkill_UI();
        }
    }

    public void UpdatePhase(int phase)
    {
        //timerTMP.text = "Time Remaining: " + remainingTime.ToString();
        phaseTMP.text = "Phase " + phase.ToString();
    }

    public void UpdatePhase(string phase)
    {
        //timerTMP.text = "Time Remaining: " + remainingTime.ToString();
        phaseTMP.text = "Phase " + phase;
    }

    public void TimeIsUp()
    {
        ResourceManager.Instance.TimeUp();
        timerTMP.text = string.Empty;
    }

    public void EndGame(string _endText)
    {
        endMenuUI.SetActive(true);
        endTextUI.text = _endText;
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public static Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
        {
            return raycastHit.point;
        }
        else
        {
            Debug.Log("Didn't Hit Anything");
            return Vector3.zero;
        }
    }

    private static bool IsOverRightPanel()
    {
        if (EventSystem.current.IsPointerOverGameObject() )
            return true;
        else
            return false;
    }

    private static bool IsOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;
        else
            return false;
    }


    /// <summary>
    /// Loops over all *.cs files in Assets/_Scripts and counts the total number of lines.
    /// </summary>
    /// <returns>Total number of lines of code found in the folder.</returns>
    public static int CountAllCSLinesInScriptsFolder()
    {
        int totalLines = 0;
        // Build the absolute path to Assets/_Scripts.
        string scriptsFolderPath = Path.Combine(Application.dataPath, "_Scripts");

        if (!Directory.Exists(scriptsFolderPath))
        {
            Debug.LogWarning("Directory not found: " + scriptsFolderPath);
            return 0;
        }

        // Get all *.cs files, searching recursively.
        string[] csFiles = Directory.GetFiles(scriptsFolderPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in csFiles)
        {
            // Read all lines of the file and count them.
            totalLines += File.ReadAllLines(file).Length;
        }

        Debug.Log("Total number of lines in .cs files in Assets/_Scripts: " + totalLines);
        return totalLines;
    }



    /// <summary>
    /// Building Management Section
    /// </summary>

    public void SetSO_Wall() { currentSO = buildingsList.WallSO;
        NewBuildingSelected();
    }
    public void SetSO_MG() { currentSO = buildingsList.MG_SO;
        NewBuildingSelected();
    }
    public void SetSO_Canon() { currentSO = buildingsList.Canon_SO;
        NewBuildingSelected();
    }
    public void SetSO_GlueGun() { currentSO = buildingsList.TurretSO;
        NewBuildingSelected();
    }
    public void SetSO_HabitatLight() { currentSO = buildingsList.habitatLightSO;
        NewBuildingSelected();
    }

    public void SetSO_Attanium() { 
        currentSO = buildingsList.attaniumSO;
        NewBuildingSelected();
    }
    public void SetSO_Marcum()
    {
        currentSO = buildingsList.marcumSO;
        NewBuildingSelected();
    }
    public void SetSO_Imear()
    {
        currentSO = buildingsList.imearSO;
        NewBuildingSelected();
    }
    public void SetSO_Scanner() { currentSO = buildingsList.ScannerSO;
        NewBuildingSelected();
    }
    public void SetSO_Lab() { currentSO = buildingsList.LabSO;
        NewBuildingSelected();
    }

    public void SetSO_BuilderBrigade()
    {
        currentSO = buildingsList.builderBrigadeSO;
        NewBuildingSelected();
    }



    private void NewBuildingSelected()
    {
        tempObjectShowing.transform.rotation = Quaternion.identity;
        tempBuildingOffset = Vector3.zero;
        tempObjectShowing.transform.localScale = new Vector3(currentSO.width, 1, currentSO.length);

        activeBuildingText.text = currentSO.visibleName;
        attActiveCostText.text = currentSO.buildingCost.x.ToString();
        marcumActiveCostText.text = currentSO.buildingCost.y.ToString();
        imearActiveCostText.text = currentSO.buildingCost.z.ToString();
    }

    public void InvertGameState()
    {
        gameRunning = !gameRunning;
    }

    public bool GetGameState()
    {
        return gameRunning;
    }
}
