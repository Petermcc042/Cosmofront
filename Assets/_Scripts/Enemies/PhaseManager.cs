using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class PhaseManager : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GameManager gameManager;

    private Dictionary<string, List<float>> phaseData;

    private int currentPhase = -1;
    private float phaseTimer;
    private bool betweenPhases = false;
    private bool tryEndGame = false;

    [SerializeField] private float defaultTimer = 10f;
    [SerializeField] private TextAsset csvFile; // Fallback if `total_spawn_time` is missing
    [SerializeField] private TextMeshProUGUI timerText; // Reference to the UI Text component
    private int lastDisplayedTime = -1; // Store the last displayed seconds

    private GameSettingsSO gameSettings;

    private void Awake()
    {
        phaseTimer = 4f;
    }

    public void InitPhaseManager(GameSettingsSO _gameSettings)
    {
        gameSettings = _gameSettings;
    }


    private void SetPhase(int phaseIndex)
    {
        if (gameSettings != null)
        {
            phaseTimer = gameSettings.phaseLengthList[currentPhase];
        }
        else
        {
            phaseTimer = defaultTimer;
        }

        // Update spawn rates, weights, etc
    }


    private float pauseTimer = 0f;
    private bool isPausing = false;

    void Update()
    {
        if (gameManager.GetGameState()) { return; }

        if (isPausing)
        {
            // Handle the pause countdown
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                ResumeSpawning();
            }
        }
        else
        {
            // Handle the phase timer
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f)
            {
                AdvancePhase();
            }
        }
        UpdateTimerUI();

        if (tryEndGame && enemyManager.enemyCount <= 0) { gameManager.EndGame("You Win"); }
    }

    private void UpdateTimerUI()
    {
        int currentSeconds = Mathf.CeilToInt(phaseTimer); // Round up to nearest second
        if (currentSeconds != lastDisplayedTime)
        {
            timerText.text = currentSeconds.ToString(); // Update UI only when the value changes
            lastDisplayedTime = currentSeconds;
        }
    }

    private void AdvancePhase()
    {
        if (betweenPhases) { return; }
        betweenPhases = true;

        currentPhase++;
        enemyManager.keepSpawning = false;

        // Check if it's the last phase
        if (currentPhase >= gameSettings.phaseLengthList.Count)
        {
            enemyManager.keepSpawning = false;
            Debug.Log("Last phase reached. Looping to phase 0.");
            tryEndGame = true;
            
            phaseTimer = 100f; // Or reset to some default value
            betweenPhases = false;
            return;
        }

        gameManager.UpdatePhase((currentPhase - 1) + " to " + currentPhase);
        Debug.Log("Pausing spawning for 5 seconds.");

        pauseTimer = 5f; // Set the pause duration
        isPausing = true; // Mark as in pause state
    }

    private void ResumeSpawning()
    {
        isPausing = false; // Exit the pause state

        // Resume spawning and set the new phase
        enemyManager.keepSpawning = true;
        enemyManager.updatePhase(currentPhase); // Update enemies to the new phase
        gameManager.UpdatePhase(currentPhase);
        SetPhase(currentPhase);

        Debug.Log("Spawning resumed.");
        betweenPhases = false;
    }
}
