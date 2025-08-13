using Moserware.Skills;
using Moserware.Skills.TrueSkill;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class VanillaTrueskillSystemManager : MonoBehaviour
{
    public static VanillaTrueskillSystemManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    [Header("TOTAL MATCHES")]
    [Tooltip("Total number of matches per player")]
    public int totalMatches = 10;

    [Header("Match Properties")]
    public int maxRoundsPerMatch = 3;
    public int teamSize = 5;
    public float matchingThreshold = 50f;

    //teams are now handled matchwise, that will make the matches be able to run simultaneously.
    //[Header("Teams")]
    //public List<Player> team1 = new();
    //public List<Player> team2 = new();

    [Header("Debug Info")]
    public bool logTeams = true;
    public TMP_Text minMatchPerPlayerText;



    [System.Serializable]
    public class PoolPlayers
    {
        [NonSerialized]
        public List<Player> playersInPool;

        public int poolSize;
        public void UpdatePoolSize(int index)
        {
            poolSize = playersInPool.Count;

            UIManager.instance.PoolPlayerCountTxtGOs[index].GetComponent<TMP_Text>().text = poolSize.ToString();
        }

        public PoolPlayers()
        {
            playersInPool = new List<Player>();
        }
    };
    [Header("Players")]
    [SerializeField]
    public PoolPlayers[] poolPlayersList;

    [Header("New Player Details")]
    public float newPlayerRating = 0;
    public bool isNewPlayerSmurf = false;

    List<double> MSEs = new();
    List<int> smurfPlayerIDs = new();
    int smurfCount = 0;

    System.Random rng = new();

    float minEloGlobal = 0; 
    float maxEloGlobal = 0; 

    private readonly FactorGraphTrueSkillCalculator calculator = new();
    private readonly GameInfo defaultGameInfo = GameInfo.DefaultGameInfo;

    int lastMSEMatchCheckpoint = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       minEloGlobal = CentralProperties.instance.eloRangePerPool[0].x;
       maxEloGlobal = CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetupTrueskillSystem(int MPP)
    {
        totalMatches = MPP;
        StartCoroutine(InitialiseTrueskillSystem(MPP));
    }

    //using Box-Muller transformation to generate normally distributed values
    float GenerateNormallyDistributedRealSkill(float min, float max)
    {
        double mean = (min + max) / 2.0;
        double stdDev = (max - min) / 6.0; // ~99.7% of values fall within range

        while (true)    //not clamping values, to preserve bell shaped nature
        {
            double u1 = 1.0 - rng.NextDouble(); // avoid 0
            double u2 = 1.0 - rng.NextDouble();
            double z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double normalised = mean + stdDev * z1;

            if (normalised >= min && normalised <= max)
                return (float)normalised;
        }
    }

    float GetTop5PercentileElo(float min, float max)
    {
        double mean = (min + max) / 2;
        double stdDev = (max - min) / 6;
        float top5PercentileMin = (float)(mean + 1.645 * stdDev);
        return UnityEngine.Random.Range(top5PercentileMin, max);
    }

    public enum RatingConversion    //helper enum for rating conversions
    {
        None,
        To_TrueSkill,
        To_MyScale,
    }
    //scaling of trueskill values to my scale
    public float ConvertRating(float rating, float min, float max, RatingConversion conversionType, bool clamped = true)
    {
        float trueSkillMin = 0f;
        float trueSkillMax = 50f;

        if (clamped)
        {
            if (conversionType == RatingConversion.To_TrueSkill)
                return Mathf.Clamp(
                    trueSkillMin + ((rating - min) / (max - min)) * (trueSkillMax - trueSkillMin),
                    trueSkillMin, trueSkillMax);
            else if (conversionType == RatingConversion.To_MyScale)
                return Mathf.Clamp(
                    min + ((rating - trueSkillMin) / (trueSkillMax - trueSkillMin)) * (max - min),
                    min, max);
        }
        else
        {
            if (conversionType == RatingConversion.To_TrueSkill)
                return trueSkillMin + ((rating - min) / (max - min)) * (trueSkillMax - trueSkillMin);
            else if (conversionType == RatingConversion.To_MyScale)
                return min + ((rating - trueSkillMin) / (trueSkillMax - trueSkillMin)) * (max - min);
        }
        return -1;
    }

    int totalPlayers = 0;
    int maxIDs = 0;
    public void AddAPlayer()
    {
        var cp = CentralProperties.instance;

        newPlayerRating = Mathf.Clamp(newPlayerRating, minEloGlobal + 50, maxEloGlobal);

        for(int i = 0; i < cp.totPools; i++)
        {
            if(newPlayerRating <= cp.eloRangePerPool[i].y && newPlayerRating >= cp.eloRangePerPool[i].x)
            {
                Player newPlayer = new();
                float realSkill;

                int ID = MainServer.instance.GenerateRandomID(totalPlayers + Mathf.FloorToInt(0.3f * totalPlayers), maxIDs);

                if (isNewPlayerSmurf)  //putting smurfs in the first pool
                {
                    realSkill = GetTop5PercentileElo(minEloGlobal, maxEloGlobal);
                    newPlayer.playerType = Player.PlayerType.Smurf;
                    smurfPlayerIDs.Add(ID);
                    smurfCount++;
                }
                else
                {
                    realSkill = GenerateNormallyDistributedRealSkill(minEloGlobal, maxEloGlobal);
                }

                newPlayer.SetPlayer(ID, newPlayerRating, realSkill,
                                        trueSkillRating:
                                            ConvertRating(newPlayerRating, minEloGlobal, maxEloGlobal, RatingConversion.To_TrueSkill),
                                    i, MPP_loc, Player.PlayerState.Idle);

                newPlayer.muHistory.Add(newPlayer.playerData.TrueSkillRating.Mean);
                newPlayer.sigmaHistory.Add(newPlayer.playerData.TrueSkillRating.StandardDeviation);
                newPlayer.conservativeValHistory.Add(ConvertRating((float)newPlayer.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));
                newPlayer.scaledRatingHistory.Add(ConvertRating((float)newPlayer.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));

                newPlayer.poolHistory.Add(i);

                poolPlayersList[i].playersInPool.Add(newPlayer);
                poolPlayersList[i].UpdatePoolSize(i);

                Debug.LogError($"new player with ID {newPlayer.playerData.Id} and rating {newPlayer.playerData.Elo} added to pool {newPlayer.playerData.Pool}\nConfirmation from pool list {poolPlayersList[i].playersInPool.Contains(newPlayer)}");

                break;
            }
        }

        totalPlayers++;
        cp.totPlayers++;

        UIManager.instance.CancelAddPlayerUI();
    }

    int MPP_loc = 0;
    IEnumerator InitialiseTrueskillSystem(int MPP)
    {
        MPP_loc = MPP;

        var cp = CentralProperties.instance;

        int[] poolPlayers = new int[cp.totPools];
        for (int i = 0; i < cp.totPools; i++)
        {
            poolPlayers[i] = Mathf.FloorToInt(cp.totPlayers * cp.playerDistributionInPools[i] / 100);

            yield return null;
        }

        poolPlayersList = new PoolPlayers[cp.totPools];
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            poolPlayersList[i] = new PoolPlayers();
        }

        totalPlayers = (int)cp.totPlayers;
        maxIDs = totalPlayers + Mathf.FloorToInt(0.2f * totalPlayers);
        int maxAttempts = totalPlayers + Mathf.FloorToInt(0.3f * totalPlayers);
        
        for (int i = 0; i < cp.totPools; i++)
        {
            float minElo = cp.eloRangePerPool[i].x;
            float maxElo = cp.eloRangePerPool[i].y;

            for (int j = 0; j < poolPlayers[i]; j++)
            {
                Player newPlayer = new();

                //starting player with a random value in the pool range (only in trueskill) because trueskill depends heavily on mean value for updates and giving everyone the same rating would lead to a very slow convergence
                //float rating = UnityEngine.Random.Range(minElo, maxElo);

                float rating = minElo + 50; //experimenting

                float realSkill = 0;
                int ID = MainServer.instance.GenerateRandomID(maxAttempts, maxIDs);

                if (i == 0 && smurfCount < CentralProperties.instance.totSmurfs)  //putting smurfs in the first pool
                {
                    realSkill = GetTop5PercentileElo(minEloGlobal, maxEloGlobal);
                    newPlayer.playerType = Player.PlayerType.Smurf;
                    smurfPlayerIDs.Add(ID);
                    smurfCount++;
                }
                else
                {
                    realSkill = GenerateNormallyDistributedRealSkill(minEloGlobal, maxEloGlobal);
                }

                newPlayer.SetPlayer(ID, rating, realSkill, 
                                        trueSkillRating: 
                                            ConvertRating(rating, minEloGlobal, maxEloGlobal, RatingConversion.To_TrueSkill),
                                    i, MPP, Player.PlayerState.Idle);

                newPlayer.muHistory.Add(newPlayer.playerData.TrueSkillRating.Mean);
                newPlayer.sigmaHistory.Add(newPlayer.playerData.TrueSkillRating.StandardDeviation);
                newPlayer.conservativeValHistory.Add(ConvertRating((float)newPlayer.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));
                newPlayer.scaledRatingHistory.Add(ConvertRating((float)newPlayer.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));

                //Debug.LogError($"{newPlayer.playerData.TrueSkillRating.Mean}\n{newPlayer.playerData.TrueSkillRating.ConservativeRating}\n{ConvertRating((float)newPlayer.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyRating)}\n{ConvertRating((float)newPlayer.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyRating)}");

                newPlayer.poolHistory.Add(i);

                poolPlayersList[i].playersInPool.Add(newPlayer);

                if (j % 100 == 0)
                {
                    poolPlayersList[i].UpdatePoolSize(i);
                    yield return null; //yielding occasionally to keep Unity responsive
                }
            }

            poolPlayersList[i].UpdatePoolSize(i);
        }

        yield return null;

        CreateAMatch();
    }

    public void CreateAMatch()
    {
        StartCoroutine(SimulateMatches());
    }

    IEnumerator CalculateMSE()
    {
        float totalError = 0f;
        int totalPlayers = CentralProperties.instance.totPlayers;

        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            var pool = poolPlayersList[i].playersInPool;
            for (int j = 0; j < pool.Count; j++)
            {
                float elo = (float)ConvertRating((float)pool[j].playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale);
                float realSkill = (float)pool[j].playerData.RealSkill;
                float error = elo - realSkill;

                totalError += error * error;

                if (j % 500 == 0)
                {
                    yield return null; //yielding occasionally to keep Unity responsive
                }
            }
        }

        float mse = totalError / totalPlayers;
        MSEs.Add(mse);
    }

    int totalMatchesSimulated = 0;
    IEnumerator SimulateMatches()
    {
        List<Player> allPlayers = new();
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            allPlayers.AddRange(poolPlayersList[i].playersInPool);
        }

        int totalRemainingPlayers = 0;
        for (int i = 0; i < allPlayers.Count; i++)
            if (allPlayers[i].playerData.MatchesToPlay > 0)
                totalRemainingPlayers++;

        StartCoroutine(CalculateMSE());

        while (totalRemainingPlayers > 0)
        {
            int randomPool = -1;
            int maxTries = 1000;

            for (int attempts = 0; attempts < maxTries; attempts++)
            {
                int tryPool = UnityEngine.Random.Range(0, CentralProperties.instance.totPools);

                if (poolPlayersList[tryPool].playersInPool.Any(p => p.playerData.MatchesToPlay > 0))
                {
                    randomPool = tryPool;
                    break;
                }
            }

            if (randomPool != -1)
            {
                StartTeamSplit(poolPlayersList[randomPool].playersInPool, randomPool, totalMatchesSimulated);
                totalMatchesSimulated++;
            }

            int minMatchesPlayed = int.MaxValue;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                minMatchesPlayed = Mathf.Min(minMatchesPlayed, allPlayers[i].playerData.GamesPlayed);
            }

            minMatchPerPlayerText.text = minMatchesPlayed.ToString();

            // Checkpoint every 5 matches
            if (minMatchesPlayed > lastMSEMatchCheckpoint && minMatchesPlayed % 5 == 0)
            {
                lastMSEMatchCheckpoint = minMatchesPlayed;
                StartCoroutine(CalculateMSE());
            }

            totalRemainingPlayers = 0;
            for (int i = 0; i < allPlayers.Count; i++)
                if (allPlayers[i].playerData.MatchesToPlay > 0)
                    totalRemainingPlayers++;

            yield return null;
        }

        StartCoroutine(CalculateMSE());
        minMatchPerPlayerText.text = MPP_loc.ToString();

        Debug.Log($"All players in all pools have completed their required matches. Total matches: {totalMatchesSimulated}");

        // Wait for final matches to complete
        yield return new WaitForSecondsRealtime(10f);

        allPlayers.Clear();
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            allPlayers.AddRange(poolPlayersList[i].playersInPool);
        }

        string time = System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
        StartCoroutine(ExportPlayerDataToCSV(allPlayers, time + $"VanillaTrueskillSystem-For-{totalMatches}Matches-PerPlayer-TotPlayerCount-{allPlayers.Count}"));
    }

    IEnumerator ExportPlayerDataToCSV(List<Player> allPlayers, string fileName)
    {
        StringBuilder csvContent = new();

        int yieldFrequency = 5000; // Yield every 5000 players
        int processedPlayers = 0;

        // CSV Header (Columns)
        csvContent.AppendLine("PlayerID,KDR,Kills,Deaths,ClutchRatio,AssistRatio,ConservativeRating,ScaledRating,RealSkill,Pool,TotalDelta,GamesPlayed,Wins,Outcomes,MuHistory,SigmaHistory,ConservativeRatingHistory,ScaledRatingHistory,PoolHistory,MSE-List,Smurfs-List,TotalMatchesSimulated");

        string MSEListStr = string.Join(";", MSEs);
        string smurfListStr = string.Join(";", smurfPlayerIDs);

        for (int i = 0; i < allPlayers.Count; ++i)
        {
            var player = allPlayers[i];

            // Serialise lists as semicolon-separated strings
            string conservativeRatingHistoryStr = string.Join(";", player.conservativeValHistory);
            string muHistoryStr = string.Join(";", player.muHistory);
            string sigmaHistoryStr = string.Join(";", player.sigmaHistory);
            string scaledRatingHistoryStr = string.Join(";", player.scaledRatingHistory);
            string outcomeHistoryStr = string.Join(";", player.playerData.Outcomes);
            string poolHistoryStr = string.Join(";", player.poolHistory);

            // Build CSV row
            string line = $"{player.playerData.Id},{player.playerData.KDR},{player.playerData.Kills},{player.playerData.Deaths},{player.playerData.ClutchRatio},{player.playerData.AssistRatio},{ConvertRating((float)player.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale)},{ConvertRating((float)player.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale)},{player.playerData.RealSkill},{player.playerData.Pool},{player.totalChangeFromStart},{player.playerData.GamesPlayed},{player.playerData.Wins},\"{outcomeHistoryStr}\",\"{muHistoryStr}\",\"{sigmaHistoryStr}\",\"{conservativeRatingHistoryStr}\",\"{scaledRatingHistoryStr}\",\"{poolHistoryStr}\",";

            if (i == 0)
            {
                line += ($"\"{MSEListStr}\",\"{smurfListStr}\",{totalMatchesSimulated}");
            }

            csvContent.AppendLine(line);

            processedPlayers++;

            if (processedPlayers % yieldFrequency == 0)
            {
                Debug.Log($"Processed {processedPlayers} players...");
                yield return null; // Let Unity breathe
            }
        }

        // Save CSV file
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".csv");
        File.WriteAllText(filePath, csvContent.ToString());

        Debug.Log($"Excel-compatible CSV successfully written to: {filePath}");

        StopAllCoroutines();
        GC.Collect();
    }

    //optimised in-place shuffle method for lists (Fisher–Yates)
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    List<T> ShuffleCopy<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    public void StartTeamSplit(List<Player> playerPool, int whichPool, int matchSim)
    {
        List<Player> team1 = new();
        List<Player> team2 = new();

        Shuffle(playerPool);

        bool teamsCreated = TrySplitFairTeamsAsync(playerPool, ref team1, ref team2);

        if ((teamsCreated))
        {
            //Debug.Log("Do the Match");
            for (int i = 0; i < team1.Count; i++)
            {
                team1[i].playerData.MatchesToPlay--;
            }
            for (int i = 0; i < team2.Count; i++)
            {
                team2[i].playerData.MatchesToPlay--;
            }
            StartCoroutine(SimulateMatch(team1, team2, matchSim));
        }
        else
        {
            Debug.LogError($"Could not find suitable teams in {whichPool}, trying again with merging another pool");
            var combinedPool = new List<Player>(poolPlayersList[whichPool].playersInPool);
            if (whichPool > 0)
            {
                combinedPool.AddRange(poolPlayersList[whichPool - 1].playersInPool);
                whichPool--;
            }
            else if (whichPool + 1 < CentralProperties.instance.totPools)
            {
                combinedPool.AddRange(poolPlayersList[whichPool + 1].playersInPool);
                whichPool++;
            }

            StartTeamSplit(combinedPool, whichPool, matchSim);
        }
    }

    void UpdatePlayerStatusForBothTeams(ref List<Player> team1, ref List<Player> team2, bool playing = false)
    {
        if (playing)
        {
            foreach (var p in team1)
            {
                p.playerState = Player.PlayerState.Playing;
            }
            foreach (var p in team2)
            {
                p.playerState = Player.PlayerState.Playing;
            }
        }

        else
        {
            foreach (var p in team1)
            {
                p.playerState = Player.PlayerState.Idle;
            }
            foreach (var p in team2)
            {
                p.playerState = Player.PlayerState.Idle;
            }
        }
    }

    IEnumerator SimulateMatch(List<Player> team1, List<Player> team2, int matchSim)
    {
        int team1RoundWins = 0;
        int team2RoundWins = 0;

        foreach (var p in team1)
        {
            p.playerData.ResetMatchData();
        }
        foreach (var p in team2)
        {
            p.playerData.ResetMatchData();
        }

        for (int round = 0; round < maxRoundsPerMatch; round++)
        {
            List<Player> team1Shuffled = ShuffleCopy(team1);
            List<Player> team2Shuffled = ShuffleCopy(team2);

            List<Player> aliveTeam1 = new List<Player>(team1Shuffled);
            List<Player> aliveTeam2 = new List<Player>(team2Shuffled);

            // Track potential clutch opportunity
            bool team1HadClutchChance = false;
            bool team2HadClutchChance = false;

            Player team1LastAlive = null;
            Player team2LastAlive = null;

            while (aliveTeam1.Count > 0 && aliveTeam2.Count > 0)
            {
                Player p1 = aliveTeam1[0];
                Player p2 = aliveTeam2[0];

                //using elo-based probability for win chance, but using realskill rather than trueskill value to predict winner, because in the game winning depends on the player's skill, not the system's
                double p1WinProb = 1.0 / (1.0 + Math.Pow(10, (p2.playerData.RealSkill - p1.playerData.RealSkill) / 400.0));

                bool p1Wins = rng.NextDouble() < p1WinProb;

                // Update rounds played
                p1.playerData.RoundsPlayed++;
                p2.playerData.RoundsPlayed++;

                if (p1Wins)
                {
                    //match statistics (that trueskill doesnt care for)
                    p1.playerData.Kills++;
                    p2.playerData.Deaths++;

                    p1.playerData.thisMatchKills++;
                    p2.playerData.thisMatchDeaths++;

                    // Check if p1 is in a clutch
                    if (aliveTeam1.Count == 1 && aliveTeam2.Count >= 2)
                    {
                        team1HadClutchChance = true;
                        team1LastAlive = aliveTeam1[0];
                    }

                    aliveTeam2.Remove(p2);

                    if (UnityEngine.Random.Range(0f, 1f) <= 0.5f) //50% chance of assist
                    {
                        //randomly select a player to award assist
                        int i = UnityEngine.Random.Range(0, aliveTeam1.Count);
                        var assistPlayer = aliveTeam1[i];
                        assistPlayer.playerData.Assists++;
                    }
                }
                else
                {
                    p1.playerData.Deaths++;
                    p2.playerData.Kills++;

                    p1.playerData.thisMatchDeaths++;
                    p2.playerData.thisMatchKills++;

                    if (aliveTeam2.Count == 1 && aliveTeam1.Count >= 2)
                    {
                        team2HadClutchChance = true;
                        team2LastAlive = aliveTeam2[0];
                    }

                    aliveTeam1.Remove(p1);

                    if (UnityEngine.Random.Range(0f, 1f) <= 0.5f) //50% chance of assist
                    {
                        //randomly select a player to award assist
                        int i = UnityEngine.Random.Range(0, aliveTeam2.Count);
                        var assistPlayer = aliveTeam2[i];
                        assistPlayer.playerData.Assists++;
                    }
                }

                p1.playerData.UpdateKDR();
                p2.playerData.UpdateKDR();

                yield return null;
            }

            // Determine round outcome and clutch success
            if (aliveTeam1.Count > 0)
            {
                team1RoundWins++;

                if (team1HadClutchChance && team1LastAlive != null)
                {
                    team1LastAlive.playerData.Clutches++;
                    team1LastAlive.playerData.ClutchesPresented++;

                    team1LastAlive.playerData.thisMatchClutches++;
                    team1LastAlive.playerData.thisMatchClutchesPresented++;
                }

                if (team2HadClutchChance && team2LastAlive != null)
                {
                    team2LastAlive.playerData.ClutchesPresented++;

                    team2LastAlive.playerData.thisMatchClutchesPresented++;
                }
            }
            else
            {
                team2RoundWins++;

                if (team2HadClutchChance && team2LastAlive != null)
                {
                    team2LastAlive.playerData.Clutches++;
                    team2LastAlive.playerData.ClutchesPresented++;

                    team2LastAlive.playerData.thisMatchClutches++;
                    team2LastAlive.playerData.thisMatchClutchesPresented++;
                }

                if (team1HadClutchChance && team1LastAlive != null)
                {
                    team1LastAlive.playerData.ClutchesPresented++;

                    team1LastAlive.playerData.thisMatchClutchesPresented++;
                }
            }

            yield return null;
        }

        int winner = 0;

        if (team1RoundWins > team2RoundWins)
        {
            Debug.Log("Team 1 wins the match!");
            winner = 1;
        }
        else if (team2RoundWins > team1RoundWins)
        {
            Debug.Log("Team 2 wins the match!");
            winner = 2;
        }

        //all trueskill cares for is who won
        UpdateTrueskill(ref team1, ref team2, winner);

        foreach (var p in team1)
        {
            p.playerData.GamesPlayed++;

            CheckRankDerank(p);

            yield return null;
        }

        foreach (var p in team2)
        {
            p.playerData.GamesPlayed++;

            CheckRankDerank(p);

            yield return null;
        }

        UpdatePlayerStatusForBothTeams(ref team1, ref team2, false);

        Debug.LogWarning(matchSim + " Match Simulation Completed!");
    }

    void CheckRankDerank(Player p)
    {
        int currentPool = p.playerData.Pool;
        double elo = ConvertRating((float)p.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale);

        int newPool = -1;
        var cp = CentralProperties.instance;

        // Check which pool the player now belongs to
        for (int i = 0; i < cp.totPools; i++)
        {
            Vector2 range = cp.eloRangePerPool[i];
            if (elo >= range.x && elo <= range.y)
            {
                newPool = i;
                break;
            }
        }

        // No matching pool? Clamp to closest
        if (newPool == -1)
        {
            if (elo < cp.eloRangePerPool[0].x)
                newPool = 0;
            else
                newPool = cp.totPools - 1;
        }

        // If pool changed, remove & reassign
        if (newPool != currentPool)
        {
            poolPlayersList[currentPool].playersInPool.Remove(p);
            poolPlayersList[currentPool].UpdatePoolSize(currentPool);
            poolPlayersList[newPool].playersInPool.Add(p);
            poolPlayersList[newPool].UpdatePoolSize(newPool);

            if (newPool > 0 && p.playerType != Player.PlayerType.Smurf)
            {
                p.playerType = Player.PlayerType.Experienced;
            }

            p.playerData.Pool = newPool;

            p.poolHistory.Add(newPool);

            Debug.Log($"Player {p.playerData.Id} moved from Pool {currentPool} to Pool {newPool} (Elo: {elo} Real Skill: {p.playerData.RealSkill})");
        }
    }

    void UpdateTrueskill(ref List<Player> team1Players, ref List<Player> team2Players, int winner)
    {
        var team1 = new Team<Player>();
        var team2 = new Team<Player>();

        for (int i = 0; i < team1Players.Count; i++)
        {
            team1.AddPlayer(team1Players[i], team1Players[i].playerData.TrueSkillRating);
            team2.AddPlayer(team2Players[i], team2Players[i].playerData.TrueSkillRating);
        }

        int[] ranks = { 1, 1 };
        if(winner == 1)
        {
            ranks = new int[] { 1, 2 };
            foreach(var p in team1Players)
            {
                p.playerData.Wins++;
                p.playerData.Outcomes.Add(1);
            }
            foreach (var p in team2Players)
            {
                p.playerData.Outcomes.Add(0);
            }
        }
        else if(winner == 2)
        {
            ranks = new int[] { 2, 1 };
            foreach(var p in team2Players)
            {
                p.playerData.Wins++;
                p.playerData.Outcomes.Add(1);

            }
            foreach (var p in team1Players)
            {
                p.playerData.Outcomes.Add(0);
            }
        }

        var teams = Teams.Concat<Player>(team1, team2).ToArray();

        var newRatings = calculator.CalculateNewRatings(defaultGameInfo, teams, ranks);

        foreach(var p in team1Players)
        {
            p.playerData.TrueSkillRating = newRatings[p];

            //clamping
            var scaledRating = ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale);
            scaledRating = Mathf.Clamp(scaledRating, minEloGlobal, maxEloGlobal);
            p.playerData.TrueSkillRating = new(ConvertRating(scaledRating, minEloGlobal, maxEloGlobal, RatingConversion.To_TrueSkill), p.playerData.TrueSkillRating.StandardDeviation);

            p.muHistory.Add(p.playerData.TrueSkillRating.Mean);
            p.sigmaHistory.Add(p.playerData.TrueSkillRating.StandardDeviation);
            p.conservativeValHistory.Add(ConvertRating((float)p.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));
            p.scaledRatingHistory.Add(ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));

            UIManager.instance.UpdateBoxContent(p);

        }

        foreach (var p in team2Players)
        {
            p.playerData.TrueSkillRating = newRatings[p];

            //clamping
            var scaledRating = ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale);
            scaledRating = Mathf.Clamp(scaledRating, minEloGlobal, maxEloGlobal);
            p.playerData.TrueSkillRating = new(ConvertRating(scaledRating, minEloGlobal, maxEloGlobal, RatingConversion.To_TrueSkill), p.playerData.TrueSkillRating.StandardDeviation);

            p.muHistory.Add(p.playerData.TrueSkillRating.Mean);
            p.sigmaHistory.Add(p.playerData.TrueSkillRating.StandardDeviation);
            p.conservativeValHistory.Add(ConvertRating((float)p.playerData.TrueSkillRating.ConservativeRating, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));
            p.scaledRatingHistory.Add(ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));

            UIManager.instance.UpdateBoxContent(p);

        }

    }

    int usingSorting = 0;
    int usingRandomSampling = 0;

    [Space(10)]
    bool flip = false;
    public bool TrySplitFairTeamsAsync(List<Player> pool, ref List<Player> team1, ref List<Player> team2)
    {
        //players available for matching must be IDLE and total played matches must be less than 3 times the total matches required (limiting the max matches played to force the system to converge)
        var idlePlayers = pool
                            .Where(p => p.playerState == Player.PlayerState.Idle && p.playerData.GamesPlayed < totalMatches * 3)
                            .OrderBy(p => p.playerData.GamesPlayed)
                            .ThenBy(p => p.playerData.TrueSkillRating.ConservativeRating)
                            .ToList();

        if (idlePlayers.Count < teamSize * 2) return false;

        //matching players based on sorted composite skill
        if (!flip)
        {
            for (int i = 0; i + teamSize * 2 <= idlePlayers.Count; i++)
            {
                var batch = idlePlayers.GetRange(i, teamSize * 2);

                var result = GetTeams(batch);
                if (result.Bool)
                {
                    team1 = result.team1;
                    team2 = result.team2;
                    UpdatePlayerStatusForBothTeams(ref team1, ref team2, true);

                    flip = true;
                    usingSorting++;

                    return true;
                }
            }

            flip = true;
        }
        if (flip)
        {
            for (int i = idlePlayers.Count - teamSize * 2; i >= 0; i--)
            {
                var batch = idlePlayers.GetRange(i, teamSize * 2);

                var result = GetTeams(batch);
                if (result.Bool)
                {
                    team1 = result.team1;
                    team2 = result.team2;
                    UpdatePlayerStatusForBothTeams(ref team1, ref team2, true);

                    flip = false;
                    usingSorting++;

                    return true;
                }
            }

            flip = false;
        }

        //sorting has failed, try random sample
        var idlePlayersShuffled = ShuffleCopy(idlePlayers);
        int maxAttempts = idlePlayersShuffled.Count * 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            HashSet<int> selectedIndices = new();

            // Select 2 * teamSize unique indices randomly from the idlePlayers list
            while (selectedIndices.Count < teamSize * 2)
            {
                int randIndex = rng.Next(idlePlayersShuffled.Count);
                selectedIndices.Add(randIndex);
            }

            // Convert to list for indexing
            var selectedPlayers = selectedIndices.Select(i => idlePlayersShuffled[i]).ToList();

            // Shuffle the selected players (small list, so fast)
            for (int i = selectedPlayers.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (selectedPlayers[i], selectedPlayers[j]) = (selectedPlayers[j], selectedPlayers[i]);
            }

            var getTeams = GetTeams(selectedPlayers);
            if (getTeams.Bool)
            {
                team1 = getTeams.team1;
                team2 = getTeams.team2;
                UpdatePlayerStatusForBothTeams(ref team1, ref team2, true);

                usingRandomSampling++;

                return true;
            }
        }

        return false;
    }

    (bool Bool, List<Player> team1, List<Player> team2) GetTeams(List<Player> somePlayers)
    {
        var allTeams = GenerateAllPossibleCombinations(somePlayers, teamSize);

        foreach(var teamPair in allTeams)
        {
            float avgT1 = teamPair.team1.Average(p => (float)ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));
            float avgT2 = teamPair.team2.Average(p => (float)ConvertRating((float)p.playerData.TrueSkillRating.Mean, minEloGlobal, maxEloGlobal, RatingConversion.To_MyScale));

            //using a threshold mechanism to match players rather than trueskill's match quality thing
            if (Mathf.Abs(avgT1 - avgT2) <= matchingThreshold)
                return (true, teamPair.team1, teamPair.team2);
        }

        return (false, null, null);
    }

    public struct AllTeams
    {
        public List<Player> team1;
        public List<Player> team2;

        public AllTeams(List<Player> t1, List<Player> t2)
        {
            team1 = t1;
            team2 = t2;
        }
    }

    public List<AllTeams> GenerateAllPossibleCombinations(List<Player> somePlayers, int teamSize)
    {
        if (somePlayers.Count < teamSize * 2)
            throw new ArgumentException($"List size must be more than {teamSize * 2}");

        var results = new List<AllTeams>();
        var uniquePairs = new HashSet<string>();

        foreach (var team1 in GetCombinations(somePlayers, teamSize))
        {
            var team2Candidates = somePlayers.Except(team1).ToList();

            if (team2Candidates.Count >= teamSize)
            {
                foreach (var team2 in GetCombinations(team2Candidates, teamSize))
                {
                    // Create a canonical key using sorted IDs
                    var t1Ids = team1.Select(p => p.playerData.Id).OrderBy(id => id);
                    var t2Ids = team2.Select(p => p.playerData.Id).OrderBy(id => id);

                    string team1Key = string.Join(",", t1Ids);
                    string team2Key = string.Join(",", t2Ids);

                    // Sort both team keys to ensure mirror pairs are treated the same
                    var key = string.Compare(team1Key, team2Key) < 0 ?
                              $"{team1Key}|{team2Key}" :
                              $"{team2Key}|{team1Key}";

                    if (!uniquePairs.Contains(key))
                    {
                        uniquePairs.Add(key);
                        results.Add(new AllTeams(new List<Player>(team1), new List<Player>(team2)));
                    }
                }
            }
        }

        return results;
    }

    // Helper function for combinations
    IEnumerable<List<T>> GetCombinations<T>(List<T> list, int k, int start = 0)
    {
        if (k == 0)
        {
            yield return new List<T>();
        }
        else
        {
            for (int i = start; i <= list.Count - k; i++)
            {
                foreach (var tail in GetCombinations(list, k - 1, i + 1))
                {
                    yield return new List<T> { list[i] }.Concat(tail).ToList();
                }
            }
        }
    }
}
