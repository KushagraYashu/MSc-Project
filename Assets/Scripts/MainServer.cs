using Moserware.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

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
        //FIDE Calculator says: A will gain 11 points, and B will lose 11 points. (keep in mind, FIDE results are rounded off to 1 decimal place)
        EloSystemManager.instance.UpdateEloForPlayer(verification: true, playerA: 1855f, playerB: 1889, resultInFavourofA: 1, vK: 20, exNo: 1);
        //Example 2: Player A with rating 2552 plays against Player B with rating 2440, B wins and both players have K = 10.
        //FIDE Calculator says: A will gain 11 points, and B will lose 11 points. (keep in mind, FIDE results are rounded off to 1 decimal place)
        EloSystemManager.instance.UpdateEloForPlayer(verification: true, playerA: 2552f, playerB: 2440f, resultInFavourofA: 0, vK: 10, exNo: 2);


        //Glicko
        //The system is being compared against the results of a published example (Glickman, 1995)
        //Suppose a player rated 1500 competes against players rated 1400, 1550 and 1700, winning the first game and losing the next two.Assume the 1500 - rated player’s rating deviation is 200, and his opponents’ are 30, 100 and 300, respectively. (Glickman, 1995).Find its new rating and RD.
        //Result from the paper: RD’ = 151.4 and R’ = 1464
        GlickoSystemManager.instance.VerifyGlicko(playerRating: 1500, playerRD: 200, opponentsRatings: new List<float> { 1400, 1550, 1700 }, opponentsRDs: new List<float> { 30, 100, 300 }, outcomes: new List<int> { 1, 0, 0 });


        //Vanilla TrueSkill
        //The system is being compared against another implementation of TrueSkill (https://github.com/sublee/trueskill)
        //Example 1: Player A: mu=25, sigma=8.333   Player B: mu = 27, sigma = 8.333. Find match quality
        //Python implementation says: 0.4421
        var player1TrueSkillRating = new Rating(25, 8.333);
        var player2TrueSkillRating = new Rating(27, 8.333);
        Team Team1 = new(new Moserware.Skills.Player("player 1"), player1TrueSkillRating);
        Team Team2 = new(new Moserware.Skills.Player("player 2"), player2TrueSkillRating);
        VanillaTrueskillSystemManager.instance.VerifyTrueSkillSystem(Team1, Team2);
        // Example 2: Team 1: Player A(mu= 30, sigma= 7), Player B(mu= 20, sigma= 8.333), Team 2: Player C(mu= 25, sigma=2), Player D(mu= 25, sigma=5). Find Match Quality
        //Python implementation says: 0.5658
        var playerATrueSkillRating = new Rating(30, 7);
        var playerBTrueSkillRating = new Rating(20, 8.333);
        var playerCTrueSkillRating = new Rating(25, 2);
        var playerDTrueSkillRating = new Rating(25, 5);
        Team1 = new();
        Team1.AddPlayer(new Moserware.Skills.Player("player A"), playerATrueSkillRating);
        Team1.AddPlayer(new Moserware.Skills.Player("player B"), playerBTrueSkillRating);
        Team2 = new();
        Team2.AddPlayer(new Moserware.Skills.Player("player C"), playerCTrueSkillRating);
        Team2.AddPlayer(new Moserware.Skills.Player("player D"), playerDTrueSkillRating);
        VanillaTrueskillSystemManager.instance.VerifyTrueSkillSystem(Team1, Team2);
    }

    public void GoBackToFirstScreen()
    {
        verificationScreen.SetActive(false);
        firstScreen.SetActive(true);
    }

    public void StartSimulation()
    {
        isSimulationStopped = false;

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

    public void ResetIDs()
    {
        allIDs.Clear();
        allIDs = new();
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

    public bool isSimulationStopped = false;
    public void StopWithSave()
    {
        switch (_systemIndex)
        {
            case 0: //Elo
                EloSystemManager.instance.stop = true;
                EloSystemManager.instance.stopWithSave = true;
                break;

            case 1: //Glicko
                GlickoSystemManager.instance.stop = true;
                GlickoSystemManager.instance.stopWithSave = true;
                break;

            case 2: //Vanilla TrueSkill (Moserware)
                VanillaTrueskillSystemManager.instance.stop = true;
                VanillaTrueskillSystemManager.instance.stopWithSave = true;
                break;

            case 3: //SmartMatch
                SmartMatchSystemManager.instance.stop = true;
                SmartMatchSystemManager.instance.stopWithSave = true;
                break;
        }

        isSimulationStopped = true;
    }


    public void StopWithoutSave()
    {
        switch (_systemIndex)
        {
            case 0: //Elo
                EloSystemManager.instance.stop = true;
                EloSystemManager.instance.doNotSave = true;
                break;

            case 1: //Glicko
                GlickoSystemManager.instance.stop = true;
                GlickoSystemManager.instance.doNotSave = true;
                break;

            case 2: //Vanilla TrueSkill (Moserware)
                VanillaTrueskillSystemManager.instance.stop = true;
                VanillaTrueskillSystemManager.instance.doNotSave = true;
                break;

            case 3: //SmartMatch
                SmartMatchSystemManager.instance.stop = true;
                SmartMatchSystemManager.instance.doNotSave = true;
                break;
        }
    }

    public void GoBack()
    {
        if (!isSimulationStopped)
        {
            StopWithSave();
        }

        UIManager.instance.SimulationScreen.SetActive(false);
        UIManager.instance.FirstScreen.SetActive(true);
    }

    public void ExitTheProject()
    {
        Invoke(nameof(Quit), 2f);
    }

    void Quit()
    {
        Application.Quit();
    }

}
