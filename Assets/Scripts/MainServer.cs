using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Simulation Variables")]
    [SerializeField] private int _systemIndex;
    [SerializeField] private int _matchesPerPlayer;

    [Header("Verification & main UI")]
    public GameObject verificationScreen;
    public GameObject firstScreen;

    public int SystemIndex
    {
        get { return _systemIndex; }
    }

    public int MatchesPerPlayer
    {
        get { return _matchesPerPlayer; }
    }

    //internal variables

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void RunSetVerifications()
    {
        firstScreen.SetActive(false);
        verificationScreen.SetActive(true);

        //Elo

        //Example 1: Player A with rating 1855 plays against Player B with rating 1889, A wins and both players have K = 20.
        //FIDE Calculator says: A will gain 11 points, and B will lose 11 points. (keep in mind, FIDE does not allow for fractional ratings, so result is rounded off)
        EloSystemManager.instance.UpdateEloForPlayer(verification: true, playerA: 1855f, playerB: 1889, resultInFavourofA: 1, vK: 20, exNo: 1);
        //Example 2: Player A with rating 2552 plays against Player B with rating 2440, B wins and both players have K = 10.
        //FIDE Calculator says: A will gain 11 points, and B will lose 11 points. (keep in mind, FIDE does not allow for fractional ratings, so result is rounded off)
        EloSystemManager.instance.UpdateEloForPlayer(verification: true, playerA: 2552f, playerB: 2440f, resultInFavourofA: 0, vK: 10, exNo: 2);


        //Glicko


    }

    public void GoBackToFirstScreen()
    {
        verificationScreen.SetActive(false);
        firstScreen.SetActive(true);
    }

    public void StartSimulation()
    {
        _systemIndex = UIManager.instance.SystemDropDown.GetComponent<TMP_Dropdown>().value;
        if (UIManager.instance.MPPInputField.GetComponent<TMP_InputField>().text == "")
        {
            _matchesPerPlayer = 1;
        }
        else
        {
            _matchesPerPlayer = int.Parse(UIManager.instance.MPPInputField.GetComponent<TMP_InputField>().text);
        }
        _matchesPerPlayer = Mathf.Clamp(_matchesPerPlayer, 1, 10000);

        UIManager.instance.FirstScreen.SetActive(false);
        UIManager.instance.SimulationScreen.SetActive(true);

        switch (_systemIndex)
        {
            case 0: // Elo System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = $"Elo System (min matches:{_matchesPerPlayer})";
                IntialiseEloSystem(_matchesPerPlayer);
                break;

            case 1: // Glicko System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = $"Glicko System (min matches:{_matchesPerPlayer})";
                IntialiseGlickoSystem(_matchesPerPlayer);
                break;

            case 2: // vanilla TrueSkill System (Moserware)
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = $"Vanilla TrueSkill System (Moserware) (min matches:{_matchesPerPlayer})";
                InitialiseVanillaTrueSkillSystem(_matchesPerPlayer);
                break;

            case 3: // SmartMatch System
                UIManager.instance.SysNameTxt.GetComponent<TMP_Text>().text = $"SmartMatch System (min matches:{_matchesPerPlayer})";
                InitialiseSmartMatchSystem(_matchesPerPlayer);
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

    public void AddNewPlayer()
    {
        switch (_systemIndex)
        {
            case 0: //Elo
                var esm = EloSystemManager.instance;

                esm.newPlayerRating = float.Parse(UIManager.instance.NewPlayerRating.GetComponent<TMP_InputField>().text);
                esm.isNewPlayerSmurf = UIManager.instance.NewPlayerSmurfCheckbox.GetComponent<Toggle>().isOn;

                esm.AddAPlayer();

                break;

            case 1: //Glicko
                var gsm = GlickoSystemManager.instance;

                gsm.newPlayerRating = float.Parse(UIManager.instance.NewPlayerRating.GetComponent<TMP_InputField>().text);
                gsm.isNewPlayerSmurf = UIManager.instance.NewPlayerSmurfCheckbox.GetComponent<Toggle>().isOn;

                gsm.AddAPlayer();
                break;

            case 2: //Vanilla TrueSkill (Moserware)
                var vtsm = VanillaTrueskillSystemManager.instance;

                vtsm.newPlayerRating = float.Parse(UIManager.instance.NewPlayerRating.GetComponent<TMP_InputField>().text);
                vtsm.isNewPlayerSmurf = UIManager.instance.NewPlayerSmurfCheckbox.GetComponent<Toggle>().isOn;

                vtsm.AddAPlayer();

                break;

            case 3: //SmartMatch
                var sm = SmartMatchSystemManager.instance;

                sm.newPlayerRating = float.Parse(UIManager.instance.NewPlayerRating.GetComponent<TMP_InputField>().text);
                sm.isNewPlayerSmurf = UIManager.instance.NewPlayerSmurfCheckbox.GetComponent<Toggle>().isOn;

                sm.AddAPlayer();

                break;
        }
    }

    
}
