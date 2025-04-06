using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private GameManager gameManager;

    public static ResourceManager Instance { get; private set; }

    public int attaniumTotal;
    public int marcumTotal;
    public int imearTotal;

    public int attPerSec;
    public int marcPerSec;
    public int imearPerSec;

    private float countdown;
    public bool isRunning;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        isRunning = true;
    }

    public void CallUpdate()
    {
        if (countdown <= 0f)
        {
            UpdateResources();
            countdown = 1f;
        }

        if(isRunning)
        {
            countdown -= Time.deltaTime;
        }
    }

    private void UpdateResources()
    {
        attaniumTotal += attPerSec;
        marcumTotal += marcPerSec;
        imearTotal += imearPerSec;
    }

    public int GetAttanium() { return attaniumTotal; }
    public int GetMalcan() { return marcumTotal; }
    public int GetImear() { return imearTotal; }

    public void AddAttanium(int _addition) { attPerSec += _addition; }
    public void AddMarcum(int _addition) { marcPerSec += _addition; }
    public void AddImear(int _addition) { imearPerSec += _addition; }

    public void SetAttanium(int _amount) 
    { 
        attaniumTotal += _amount;
    }

    public void SubtractResources(int _attAmount, int _malAmount, int imearAmount)
    {
        attaniumTotal -= _attAmount;
        marcumTotal -= _malAmount;
        imearTotal -= imearAmount;
    }
}
