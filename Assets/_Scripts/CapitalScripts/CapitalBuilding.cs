using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CapitalBuilding : MonoBehaviour
{
    [SerializeField] CapitalBuildingSO buildingSO;
    [SerializeField] private GameObject menuUI;

    public void OpenMenu()
    {
        menuUI.SetActive(true);
    }

    public void CloseMenu()
    {
        menuUI.SetActive(false);
    }

    public void OpenWorldView()
    {
        SceneManager.LoadScene("3_WorldView");
    }
}
