using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.LowLevel;
using static VanillaTrueskillSystemManager;

public class SmartMatchSystemManager : MonoBehaviour
{
    public static SmartMatchSystemManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    [Header("TOTAL MATCHES")]
    [Tooltip("Matches per Player")]
    public int totalMatches = 1000;

    [Header("Match Properties")]
    public int maxRoundsPerMatch;
    public int teamSize = 5;
    public float matchingThreshold = 10f;
    public float losingStreakThreshold = 10f;

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

    int lastMSEMatchCheckpoint = 0;

    float minEloGlobal;
    float maxEloGlobal;

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

    public struct TeamSplitResult
    {
        public List<Player> team1;
        public List<Player> team2;
        public bool success;
    }


    public void SetupSmartMatchSystem(int MPP)
    {
        totalMatches = MPP;
        StartCoroutine(InitialiseSmartMatchSystem(MPP));
    }

    //using Box-Muller transformation to generate normally distributed values
    float GenerateNormallyDistributedRealSkill(float min, float max)
    {
        double mean = (min + max) / 2.0;
        double stdDev = (max - min) / 6.0; // ~99.7% of values fall within range

        while (true)
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

    int totalPlayers = 0;
    int maxIDs = 0;
    public void AddAPlayer()
    {
        var cp = CentralProperties.instance;

        newPlayerRating = Mathf.Clamp(newPlayerRating, minEloGlobal, maxEloGlobal);

        for (int i = 0; i < cp.totPools; i++)
        {
            if (newPlayerRating <= cp.eloRangePerPool[i].y && newPlayerRating >= cp.eloRangePerPool[i].x)
            {
                float minElo = cp.eloRangePerPool[i].x;
                float maxElo = cp.eloRangePerPool[i].y;

                Player newPlayer = new();

                float realSkill;

                int ID = MainServer.instance.GenerateRandomID(totalPlayers + Mathf.FloorToInt(0.3f * totalPlayers), maxIDs);

                if (isNewPlayerSmurf)  //putting smurfs in the first pool
                {
                    realSkill = GetTop5PercentileElo(minEloGlobal, maxEloGlobal);
                    newPlayer.playerType = Player.PlayerType.Smurf;
                    newPlayer.playerPlayStyles.Add(Player.PlayerPlayStyle.Fragger);
                    smurfPlayerIDs.Add(ID);
                    smurfCount++;
                }
                else
                {
                    realSkill = GenerateNormallyDistributedRealSkill(minEloGlobal, maxEloGlobal);
                }

                newPlayer.SetPlayer(ID,
                                    newPlayerRating,
                                    realSkill,
                                    i,
                                    MPP_loc,
                                    Player.PlayerState.Idle,
                                    smartSys: true);

                if (i == 0)
                    newPlayer.playerPlayStyles.Add(Player.PlayerPlayStyle.Basic);

                newPlayer.EloHistory.Add((float)newPlayer.playerData.CompositeSkill);
                newPlayer.poolHistory.Add(i);

                poolPlayersList[i].playersInPool.Add(newPlayer);
                poolPlayersList[i].UpdatePoolSize(i);

                Debug.LogError($"new player with ID {newPlayer.playerData.Id} and rating {newPlayer.playerData.CompositeSkill} added to pool {newPlayer.playerData.Pool}\nConfirmation from pool list {poolPlayersList[i].playersInPool.Contains(newPlayer)}");

                break;
            }
        }

        totalPlayers++;
        cp.totPlayers++;

        UIManager.instance.CancelAddPlayer();
    }

    int MPP_loc = 0;
    IEnumerator InitialiseSmartMatchSystem(int MPP)
    {
        UIManager.instance.UpdateSmartSystemConfigurations();

        var cp = CentralProperties.instance;

        MPP_loc = MPP;

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

                float elo = minElo + 50;
                float realSkill = 0;
                int ID = MainServer.instance.GenerateRandomID(maxAttempts, maxIDs);

                if (i == 0 && smurfCount < CentralProperties.instance.totSmurfs)  //putting smurfs in the first pool
                {
                    realSkill = GetTop5PercentileElo(minEloGlobal, maxEloGlobal);
                    newPlayer.playerType = Player.PlayerType.Smurf;
                    newPlayer.playerPlayStyles.Add(Player.PlayerPlayStyle.Fragger);
                    smurfPlayerIDs.Add(ID);
                    smurfCount++;
                }
                else
                {
                    realSkill = GenerateNormallyDistributedRealSkill(minEloGlobal, maxEloGlobal);
                }

                newPlayer.SetPlayer(ID,
                                    elo,
                                    realSkill,
                                    i,
                                    MPP,
                                    Player.PlayerState.Idle,
                                    true);

                if (i == 0)
                    newPlayer.playerPlayStyles.Add(Player.PlayerPlayStyle.Basic);

                newPlayer.EloHistory.Add((float)newPlayer.playerData.CompositeSkill);
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
                float elo = (float)pool[j].playerData.CompositeSkill;
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

            //if (Time.frameCount % 1000 == 0)
            //    GC.Collect();

            yield return null;
        }

        StartCoroutine(CalculateMSE());

        Debug.Log($"All players in all pools have completed their required matches. Total matches: {totalMatchesSimulated}");

        // Wait for final matches to complete
        yield return new WaitForSecondsRealtime(10f);

        allPlayers.Clear();
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            allPlayers.AddRange(poolPlayersList[i].playersInPool);
        }

        string time = System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
        StartCoroutine(ExportPlayerDataToCSV(allPlayers, time + $"SmartMatchSystem-For-{totalMatches}Matches-PerPlayer-TotPlayerCount-{allPlayers.Count}"));
    }

    IEnumerator ExportPlayerDataToCSV(List<Player> allPlayers, string fileName)
    {
        StringBuilder csvContent = new();

        int yieldFrequency = 5000; // Yield every 5000 players
        int processedPlayers = 0;

        // CSV Header (Columns)
        csvContent.AppendLine("PlayerID,KDA,Kills,Deaths,Clutches,Assists,CS,RealSkill,PMHistory,Pool,TotalDelta,GamesPlayed,Wins,Outcomes,CSHistory,PoolHistory,MSE-List,Smurfs-List,TotalMatchesSimulated");

        string MSEListStr = string.Join(";", MSEs);
        string smurfListStr = string.Join(";", smurfPlayerIDs);


        for (int i = 0; i < allPlayers.Count; ++i)
        {
            var player = allPlayers[i];

            // Serialise lists as semicolon-separated strings
            string CSHistoryStr = string.Join(";", player.EloHistory);
            string poolHistoryStr = string.Join(";", player.poolHistory);
            string outcomeHistoryStr = string.Join(";", player.playerData.Outcomes);
            string PMHistoryStr = string.Join(";", player.playerData.PerformanceMultipliers);

            // Build CSV row
            string line = $"{player.playerData.Id},{player.playerData.KDR},{player.playerData.Kills},{player.playerData.Deaths},{player.playerData.Clutches},{player.playerData.Assists},{player.playerData.CompositeSkill},{player.playerData.RealSkill},\"{PMHistoryStr}\",{player.playerData.Pool},{player.totalChangeFromStart},{player.playerData.GamesPlayed},{player.playerData.Wins},\"{outcomeHistoryStr}\",\"{CSHistoryStr}\",\"{poolHistoryStr}\",";

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

    //optimised shuffle method for lists (Fisher–Yates)
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

        bool result = TrySplitFairTeams(playerPool, ref team1, ref team2);

        if (result)
        {
            foreach (var p in team1) p.playerData.MatchesToPlay--;
            foreach (var p in team2) p.playerData.MatchesToPlay--;

            // Set players to playing state
            UpdatePlayerStatusForBothTeams(ref team1, ref team2, true);

            StartCoroutine(SimulateMatch(team1, team2, matchSim));
        }
        else
        {
            Debug.LogError($"Could not find suitable teams in {whichPool}, trying again with merging another pool");
            var combinedPool = new List<Player>(poolPlayersList[whichPool].playersInPool);
            if (whichPool > 1)
            {
                combinedPool.AddRange(poolPlayersList[whichPool - 1].playersInPool);
                whichPool--;
            }
            else if (whichPool + 1 < CentralProperties.instance.totPools)
            {
                combinedPool.AddRange(poolPlayersList[whichPool + 1].playersInPool);
                whichPool++;
            }

            // Recursively try with expanded pool
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

        foreach(var p in team1)
        {
            p.playerData.ResetMatchData();
        }
        foreach(var p in team2)
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

                double p1WinProb = 1.0 / (1.0 + Math.Pow(10, (p2.playerData.RealSkill - p1.playerData.RealSkill) / 400.0));

                bool p1Wins = rng.NextDouble() < p1WinProb;

                // Update rounds played
                p1.playerData.RoundsPlayed++;
                p2.playerData.RoundsPlayed++;

                if (p1Wins)
                {
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

                    if(UnityEngine.Random.Range(0f, 1f) <= 0.5f) //50% chance of assist
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

        //Debug.Log("Updating Elo Ratings...");
        // Elo update for team 1
        foreach (var p in team1)
        {
            p.playerData.GamesPlayed++;

            UpdateEloForPlayer(1, p, winner, team2);

            CheckRankDerank(p);

            yield return null;
        }

        // Elo update for team 2
        foreach (var p in team2)
        {
            p.playerData.GamesPlayed++;

            UpdateEloForPlayer(2, p, winner, team1);

            CheckRankDerank(p);

            yield return null;
        }

        UpdatePlayerStatusForBothTeams(ref team1, ref team2, false);

        Debug.LogWarning(matchSim + " Match Simulation Completed!");
    }

    void CheckRankDerank(Player p)
    {
        int currentPool = p.playerData.Pool;
        double elo = p.playerData.CompositeSkill;

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

            Debug.Log($"Player {p.playerData.Id} moved from Pool {currentPool} to Pool {newPool} (Elo: {elo})");
        }
    }

    void UpdateEloForPlayer(int team, Player p, int winner, List<Player> otherTeam)
    {
        double oldCS = p.playerData.CompositeSkill;

        double expectedScore = 0.0f;
        foreach(var pT2 in otherTeam)
        {
            expectedScore += 1.0 / (1.0 + Math.Pow(10, (pT2.playerData.CompositeSkill - p.playerData.CompositeSkill) / 400.0));
        }
        expectedScore /= otherTeam.Count;

        int K;
        //K value inspired by FIDE (Federation Internationale des Echecs or World Chess Federation) regulations
        if (p.playerData.GamesPlayed <= 30)
            K = 40;
        else
        {
            if (p.playerData.Elo < 2400f)
                K = 20;
            else
                K = 10;
        }

        double actualResult = winner == team ? 1.0 : 0.0;

        if (actualResult == 1.0) { 
            p.playerData.Wins++;
            p.playerData.Outcomes.Add(1);
        }
        else
        {
            p.playerData.Outcomes.Add(0);
        }

        double delta = K * (actualResult - expectedScore);

        double performance = CalculatePerformanceMultiplier(p);

        p.playerData.PerformanceMultipliers.Add((float)performance);

        if (delta <= 0)
        {
            delta /= performance; //less impact if player performed well
        }
        else
        {
            delta *= performance; //boost in case of good performance
        }


        p.playerData.UpdateCompositeSkillAndElo(delta, (int)actualResult);

        p.EloHistory.Add((float)p.playerData.CompositeSkill);
        p.totalChangeFromStart += (float)(p.playerData.CompositeSkill - oldCS);

        UIManager.instance.UpdateBoxContent(p);


        //Debug.Log($"Team {team}\nPlayer {p.playerData.Id} (Pool {poolIndex}) Elo updated: {p.playerData.Elo} (Delta: {delta})");
    }

    double CalculatePerformanceMultiplier(Player player)
    {
        player.playerData.CalculateMatchData();

        double matchPerformance = player.playerData.CalculateMatchPerformance();

        if (matchPerformance > 2.5) return 2.0;
        if (matchPerformance > 2.0) return 1.7;
        if (matchPerformance > 1.5) return 1.5;
        if (matchPerformance < 0.5) return 0.8;
        return 1.0;
    }

    float CalcTeamElo(List<Player> team)
    {
        var sorted = team.OrderByDescending(p => p.playerData.CompositeSkill).ToList();

        // Apply weights
        // values are from
        // Wu, R., Meng, X., Chen, H., Zhu, Z. and Wang, B., 2024. Achieving fairness in team - based FPS games: A skill-based matchmaking solution. Applied and Computational Engineering, 44, pp.208 - 223.
        double weightedElo = 0.0;
        weightedElo += sorted[0].playerData.CompositeSkill * 0.35; // Highest
        weightedElo += sorted[4].playerData.CompositeSkill * 0.20; // Lowest

        for (int i = 1; i <= 3; i++) // Middle three
        {
            weightedElo += sorted[i].playerData.CompositeSkill * 0.15;
        }

        return (float)weightedElo;
    }

    //used in earlier approaches when system depended on random selection rather than sorting
    private List<Player> RandomSample(List<Player> list, int count)
    {
        return list.OrderBy(x => rng.Next()).Take(count).ToList();
    }

    int usingSorting = 0;
    int usingRandomSampling = 0;

    [Space(10)]
    bool flip = false;
    public bool TrySplitFairTeams(List<Player> pool, ref List<Player> team1, ref List<Player> team2)
    {
        //players available for matching must be IDLE
        var idlePlayers = pool
                            .Where(p => p.playerState == Player.PlayerState.Idle && p.playerData.GamesPlayed < totalMatches * 3)
                            .OrderBy(p => p.playerData.GamesPlayed)
                            .ThenBy(p => p.playerData.CompositeSkill)
                            .ToList();

        if (idlePlayers.Count < teamSize * 2) return false;

        //matching players based on sorted composite skill
        if (!flip)
        {
            for (int i = 0; i + teamSize * 2 <= idlePlayers.Count; i++)
            {
                var batch = idlePlayers.GetRange(i, teamSize * 2);

                var result = CheckLosingStreakBiasAndFairness(batch);
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

                var result = CheckLosingStreakBiasAndFairness(batch);
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

            var getTeams = CheckLosingStreakBiasAndFairness(selectedPlayers);
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

    (bool Bool, List<Player> team1, List<Player> team2) CheckLosingStreakBiasAndFairness(List<Player> somePlayers)
    {
        var allTeams = GenerateAllPossibleCombinations(somePlayers, teamSize);

        foreach(var teamPair in allTeams)
        {
            var t1Streak = IsOnLosingStreak(teamPair.team1); // returns (Bool, Count)
            var t2Streak = IsOnLosingStreak(teamPair.team2);

            float t1Elo = CalcTeamElo(teamPair.team1);
            float t2Elo = CalcTeamElo(teamPair.team2);

            if (t1Streak.Bool && !t2Streak.Bool)    //only team 1 has a losing streak
            {
                if ((t1Elo - t2Elo) >= losingStreakThreshold)
                    return (true, teamPair.team1, teamPair.team2);
            }
            else if (!t1Streak.Bool && t2Streak.Bool)   //only team 2 has a losing streak
            {
                if ((t2Elo - t1Elo) >= losingStreakThreshold)
                    return (true, teamPair.team1, teamPair.team2);
            }
            else if (t1Streak.Bool && t2Streak.Bool)    //both team have losing streaks
            {
                if (t1Streak.Count > t2Streak.Count)    //team 1 has more players on losing streak
                {
                    if ((t1Elo - t2Elo) >= losingStreakThreshold)
                        return (true, teamPair.team1, teamPair.team2);
                }
                else if (t2Streak.Count > t1Streak.Count)   //team 2 has more players on losing streak
                {
                    if ((t2Elo - t1Elo) >= losingStreakThreshold)
                        return (true, teamPair.team1, teamPair.team2);
                }
                else // Equal streak count
                {
                    if (Mathf.Abs(t1Elo - t2Elo) <= matchingThreshold)
                        return (true, teamPair.team1, teamPair.team2);
                }
            }
            // Neither team has streaks
            else if (Mathf.Abs(t1Elo - t2Elo) <= matchingThreshold)
                return (true, teamPair.team1, teamPair.team2);
        }

        return (false, null, null);
    }


    public (bool Bool, int Count) IsOnLosingStreak(List<Player> players)
    {
        int playersOnLosingStreak = 0;
        int losingStreakThreshold = 3;

        foreach (var player in players)
        {
            int count = 0;
            var outcomes = player.playerData.Outcomes;
            for (int i = outcomes.Count - 1; i >= 0; i--)
            {
                if (outcomes[i] == 0)
                {
                    count++;
                    if (count >= losingStreakThreshold)
                    {
                        playersOnLosingStreak++;
                        break;
                    }
                }
                else break;
            }
        }

        if(playersOnLosingStreak > 0) return (true, playersOnLosingStreak);
        else return (false, 0);
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


    //earlier approaches used this method, but it was not efficient for larger pools
    //now we just use a random sampling approach based on attempts rather than creating every single possible combination of players
    private List<List<T>> GenerateCombinations<T>(List<T> list, int comboSize)
    {
        List<List<T>> result = new List<List<T>>();
        GenerateCombinationsRecursive(list, comboSize, 0, new List<T>(), result);
        return result;
    }

    private void GenerateCombinationsRecursive<T>(List<T> list, int comboSize, int start, List<T> current, List<List<T>> result)
    {
        if (current.Count == comboSize)
        {
            result.Add(new List<T>(current));
            return;
        }

        for (int i = start; i < list.Count; i++)
        {
            current.Add(list[i]);
            GenerateCombinationsRecursive(list, comboSize, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }
}
