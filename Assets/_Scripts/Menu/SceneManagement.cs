using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManagement : MonoBehaviour
{
    [SerializeField] private GameSettingsSO level_oneSO;
    [SerializeField] private Slider levelLoadBar;
    [SerializeField] private GameObject levelLoadScreen;
    public Slider spawnRate;
    public TextMeshProUGUI spawnRateText;

    private void Awake()
    {
        spawnRate.value = 1;
        spawnRate.maxValue = 1; spawnRate.minValue = 0.001f;
    }

    public void UpdateDifficulty(GameSettingsSO _levelSettings, float _newDifficulty)
    {
        float temp = 1f;
        for (int i = 0; i < _levelSettings.spawnIntervalList.Count; i++)
        {
            _levelSettings.spawnIntervalList[i] = _newDifficulty * temp;
            temp -= 0.2f;
        }
    }

    public void UpdateSpawnText()
    {
        spawnRateText.text = spawnRate.value.ToString();
    }

    public void LoadLevelOne()
    {
        UpdateDifficulty(level_oneSO, spawnRate.value);

        StartCoroutine(LoadAsync("2_Capital"));
    }

    IEnumerator LoadAsync(string _sceneName)
    {
        levelLoadScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(_sceneName);
        
        while (!operation.isDone) 
        {
            float progress = Mathf.Clamp01(operation.progress / .9f);
            levelLoadBar.value = progress;
            yield return null;
        }
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("1_Menu");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
