using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainServer : MonoBehaviour
{
    public static MainServer instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Player Counts")]
    public int totPlayerCount;
    public int expPlayerCount;
    public int newbiePlayerCount;
    public int botPlayerCount;

    [Header("Player Default Props")]
    public double baseElo = 1000;
    public double matchingThreshold = 50;
    public Player.PlayerState playerState = Player.PlayerState.Idle;

    [Header("Players")]
    public List<GameObject> players = new List<GameObject>();
    public List<GameObject> newbies = new List<GameObject>();
    public List<GameObject> bots = new List<GameObject>();

    [Header("Spawn System")]
    public float spacing;
    public Transform startPosition;
    public Transform columnEndPosition;
    public Transform rowEndPosition;
    public TMP_InputField totPlayerCount_IF;
    public TMP_InputField expPlayerCount_IF;

    //internal variables

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void StartSimulation()
    {
        int systemIndex = UIManager.instance.SystemDropDown.GetComponent<TMP_Dropdown>().value;
        int matchesPerPlayer = 0;
        if (UIManager.instance.MPPInputField.GetComponent<TMP_InputField>().text == "")
        {
            matchesPerPlayer = 1;
        }
        else
        {
            matchesPerPlayer = int.Parse(UIManager.instance.MPPInputField.GetComponent<TMP_InputField>().text);
        }
        matchesPerPlayer = Mathf.Clamp(matchesPerPlayer, 1, 10000);

        UIManager.instance.FirstScreen.SetActive(false);
        UIManager.instance.SimulationScreen.SetActive(true);

        switch (systemIndex)
        {
            case 0: // Elo System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = "Elo System";
                IntialiseEloSystem(matchesPerPlayer);
                break;

            case 1: // Glicko System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = "Glicko System";
                IntialiseGlickoSystem(matchesPerPlayer);
                break;

            case 2: // vanilla TrueSkill System (Moserware)
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = "Vanilla TrueSkill System (Moserware)";
                InitialiseVanillaTrueSkillSystem(matchesPerPlayer);
                break;

            case 3: // SmartMatch System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = "SmartMatch System";
                InitialiseSmartMatchSystem(matchesPerPlayer);
                break;

        }
    }

    public void IntialiseEloSystem(int MPP)
    {
        EloSystemManager.instance.SetupEloSystem(MPP);
    }

    public void IntialiseGlickoSystem(int MPP)
    {
        GlickoSystemManager.instance.SetupGlickoSystem(MPP);
    }

    public void InitialiseVanillaTrueSkillSystem(int MPP)
    {
        VanillaTrueskillSystemManager.instance.SetupTrueskillSystem(MPP);
    }

    public void InitialiseSmartMatchSystem(int MPP)
    {
        SmartMatchSystemManager.instance.SetupSmartMatchSystem(MPP); ;
    }

    private HashSet<int> allIDs = new();
    private System.Random rng = new();
    public int GenerateRandomID(int maxAttempts, int maxIDs)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++) // prevent infinite loop
        {
            int id = rng.Next(0, maxIDs);
            if (!allIDs.Contains(id))
            {
                allIDs.Add(id);
                return id;
            }
        }

        throw new Exception("ID pool exhausted.");
    }
}
