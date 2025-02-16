using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private GameManager gameManager;

    public static ResourceManager Instance { get; private set; }

    [Header("Resource Management")]
    [SerializeField] private TextMeshProUGUI attaniumTMP;
    [SerializeField] private TextMeshProUGUI marcumTMP;
    [SerializeField] private TextMeshProUGUI imearTMP;
    [SerializeField] private TextMeshProUGUI attaniumPerSecTMP;
    [SerializeField] private TextMeshProUGUI marcumPerSecTMP;
    [SerializeField] private TextMeshProUGUI imearPerSecTMP;

    [SerializeField] private int attaniumTotal;
    [SerializeField] private int marcumTotal;
    [SerializeField] private int imearTotal;

    [SerializeField] private int attPerSec;
    [SerializeField] private int marcPerSec;
    [SerializeField] private int imearPerSec;

    private float countdown;


    private void Awake()
    {
        // Check if an instance already exists; destroy this object if one exists
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }   

        // Set the instance to this object
        Instance = this;

        //Invoke("UpdateResources", 1f);
    }

    public void CallUpdate()
    {
        if (countdown <= 0f)
        {
            UpdateResources();
            countdown = 1f;
        }

        countdown -= Time.deltaTime;
    }

    private void UpdateResources()
    {
        attaniumTotal += attPerSec;
        marcumTotal += marcPerSec;
        imearTotal += imearPerSec;

        attaniumTMP.text = attaniumTotal.ToString();
        marcumTMP.text = marcumTotal.ToString();
        imearTMP.text = imearTotal.ToString();

        attaniumPerSecTMP.text = attPerSec.ToString();
        marcumPerSecTMP.text = marcPerSec.ToString();
        imearPerSecTMP.text = imearPerSec.ToString();
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
        attaniumTMP.text = attaniumTotal.ToString();
    }

    public void SubtractResources(int _attAmount, int _malAmount, int imearAmount)
    {
        attaniumTotal -= _attAmount;
        attaniumTMP.text = attaniumTotal.ToString();
        marcumTotal -= _malAmount;
        marcumTMP.text = marcumTotal.ToString();
        imearTotal -= imearAmount;
        imearTMP.text = imearTotal.ToString();
    }

    public void TimeUp()
    {
        //CancelInvoke("UpdateResources");
    }
}
