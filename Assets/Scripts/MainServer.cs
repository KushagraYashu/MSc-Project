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

    public void IntialiseEloSystem()
    {
        EloSystemManager.instance.SetupEloSystem();
    }

    public void IntialiseGlickoSystem()
    {
        GlickoSystemManager.instance.SetupGlickoSystem();
    }

    public void InitialiseVanillaTrueSkillSystem()
    {
        VanillaTrueskillSystemManager.instance.SetupTrueskillSystem();
    }

    public void InitialiseCustomTrueSkillSystem()
    {
        CustomTrueskillSystemManager.instance.SetupCustomTrueskillSystem();
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
