using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class EloSystemManager : MonoBehaviour
{
    public static EloSystemManager instance;
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
    public float eloThreshold = 10f;

    //teams are now handled matchwise, that will make the matches be able to run simultaneously.
    //[Header("Teams")]
    //public List<Player> team1 = new();
    //public List<Player> team2 = new();

    [Header("Debug Info")]
    public bool logTeams = true;

    [System.Serializable]
    public class PoolPlayers
    {
        [NonSerialized]
        public List<Player> playersInPool;

        public int poolSize;
        public void UpdatePoolSize()
        {
            poolSize = playersInPool.Count;
        }

        public PoolPlayers()
        {
            playersInPool = new List<Player>();
        }
    };

    [Header("Players")]
    [SerializeField]
    public PoolPlayers[] poolPlayersList;

    List<double> MSEs = new();
    List<int> smurfPlayerIDs = new();
    int smurfCount = 0;

    System.Random rng = new();

    int lastMSEMatchCheckpoint = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetupEloSystem()
    {
        StartCoroutine(InitialiseEloSystem());
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

    IEnumerator InitialiseEloSystem()
    {
        var cp = CentralProperties.instance;

        int[] poolPlayers = new int[cp.totPools];
        for(int i=0; i < cp.totPools; i++)
        {
            poolPlayers[i] = Mathf.FloorToInt(cp.totPlayers * cp.playerDistributionInPools[i]/100);

            yield return null;
        }

        poolPlayersList = new PoolPlayers[cp.totPools];
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            poolPlayersList[i] = new PoolPlayers();
        }

        int totalPlayers = (int)cp.totPlayers;
        int maxIDs = totalPlayers + Mathf.FloorToInt(0.2f * totalPlayers);
        int maxAttempts = totalPlayers + Mathf.FloorToInt(0.3f * totalPlayers);
        float minEloGlobal = cp.eloRangePerPool[0].x;
        float maxEloGlobal = cp.eloRangePerPool[cp.totPools - 1].y;
        for (int i = 0; i < cp.totPools; i++)
        {
            float minElo = cp.eloRangePerPool[i].x;
            float maxElo = cp.eloRangePerPool[i].y;

            for (int j = 0; j < poolPlayers[i]; j++)
            {
                float elo = minElo;
                float realSkill = 0;
                int ID = MainServer.instance.GenerateRandomID(maxAttempts, maxIDs);

                if (i == 0 && smurfCount < CentralProperties.instance.totSmurfs)  //putting smurfs in the first pool
                {
                    realSkill = GetTop5PercentileElo(minEloGlobal, maxEloGlobal);
                    smurfPlayerIDs.Add(ID);
                    smurfCount++;
                }
                else
                {
                    realSkill = GenerateNormallyDistributedRealSkill(minEloGlobal, maxEloGlobal);
                }

                Player newPlayer = new();
                newPlayer.SetPlayer(ID,
                                    elo,
                                    realSkill,
                                    i,
                                    eloThreshold,
                                    Player.PlayerState.Idle,
                                    (i > 0) ? Player.PlayerType.Experienced : Player.PlayerType.Newbie);

                newPlayer.playerData.MatchesToPlay = totalMatches;
                newPlayer.EloHistory.Add((float)newPlayer.playerData.Elo);
                newPlayer.poolHistory.Add(i);

                poolPlayersList[i].playersInPool.Add(newPlayer);

                if (j % 100 == 0)
                {
                    poolPlayersList[i].UpdatePoolSize();
                    yield return null; //yielding occasionally to keep Unity responsive
                }
            }

            poolPlayersList[i].UpdatePoolSize();
        }

        yield return null;
    }

    public void CreateAMatch()
    {
        StartCoroutine(SimulateMatches());
    }

    IEnumerator CalculateMSE()
    {
        float totalError = 0f;
        uint totalPlayers = CentralProperties.instance.totPlayers;

        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            var pool = poolPlayersList[i].playersInPool;
            for (int j = 0; j < pool.Count; j++)
            {
                float elo = (float)pool[j].playerData.Elo;
                float realSkill = (float)pool[j].playerData.RealSkill;
                float error = elo - realSkill;

                totalError += error * error;

                if(j % 500 == 0)
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
        for(int i = 0; i < poolPlayersList.Length; i++)
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
            int randomPool = UnityEngine.Random.Range(0, CentralProperties.instance.totPools);

            StartTeamSplit(poolPlayersList[randomPool].playersInPool, randomPool, totalMatchesSimulated);
            totalMatchesSimulated++;

            int minMatchesPlayed = int.MaxValue;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                minMatchesPlayed = Mathf.Min(minMatchesPlayed, allPlayers[i].playerData.GamesPlayed);
            }

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
        StartCoroutine(ExportPlayerDataToCSV(allPlayers, time + $"EloSystem-For-{totalMatches}Matches-PerPlayer-TotPlayerCount-{allPlayers.Count}"));
    }

    IEnumerator ExportPlayerDataToCSV(List<Player> allPlayers, string fileName)
    {
        StringBuilder csvContent = new();

        int yieldFrequency = 5000; // Yield every 5000 players
        int processedPlayers = 0;

        // CSV Header (Columns)
        csvContent.AppendLine("PlayerID,Elo,RealSkill,Pool,TotalDelta,GamesPlayed,Wins,EloHistory,PoolHistory,MSE-List,Smurfs-List,TotalMatchesSimulated");

        string MSEListStr = string.Join(";", MSEs);
        string smurfListStr = string.Join(";", smurfPlayerIDs);


        for (int i = 0; i < allPlayers.Count; ++i)
        {
            var player = allPlayers[i];

            // Serialise lists as semicolon-separated strings
            string eloHistoryStr = string.Join(";", player.EloHistory);
            string poolHistoryStr = string.Join(";", player.poolHistory);

            // Build CSV row
            string line = $"{player.playerData.Id},{player.playerData.Elo},{player.playerData.RealSkill},{player.playerData.Pool},{player.totalChangeFromStart},{player.playerData.GamesPlayed},{player.playerData.Wins},\"{eloHistoryStr}\",\"{poolHistoryStr}\",";

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

        bool teamsCreated = TrySplitFairTeamsAsync(playerPool, ref team1, ref team2);

        if ((teamsCreated))
        {
            //Debug.Log("Do the Match");
            for(int i = 0; i < team1.Count; i++)
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
            Debug.LogError("Could not find suitable teams, trying again with merging another pool");
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
        int team1wins = 0;
        int team2wins = 0;

        for(int round = 0; round < maxRoundsPerMatch; round++)
        {
            List<Player> team1Shuffled = ShuffleCopy(team1);
            List<Player> team2Shuffled = ShuffleCopy(team2);

            int team1Score = 0;
            int team2Score = 0;

            //1v1s
            for (int i = 0; i < team1.Count; i++)
            {
                Player p1 = team1Shuffled[i];
                Player p2 = team2Shuffled[i];

                //calculating win probability based on real skill rather than elo
                double p1WinProb = 1.0 / (1.0 + Math.Pow(10, (p2.playerData.RealSkill - p1.playerData.RealSkill) / 400.0));

                double roll = rng.NextDouble();
                bool p1Win = roll < p1WinProb;

                if (p1Win)
                {
                    team1Score++;
                }
                else
                {
                    team2Score++;
                }

                if (i == team1.Count - 1)
                    yield return null;

                //yield return null;
                //yield return new WaitForSeconds(0.5f); // Simulate a delay for each 1v1
            }

            if(team1Score > team2Score)
            {
                team1wins++;
            }
            else
            {
                team2wins++;
            }

            //Debug.Log($"Round {round + 1}: Team 1: {team1Score}, Team 2: {team2Score}");

            yield return null;
            //yield return new WaitForSeconds(1f); // Simulate a delay for each round
        }

        int winner = 0;

        if (team1wins > team2wins)
        {
            Debug.Log("Team 1 wins the match!");
            winner = 1;
        }
        else if (team2wins > team1wins)
        {
            Debug.Log("Team 2 wins the match!");
            winner = 2;
        }

        float avgEloTeam1 = 0;
        for(int i=0;i< team1.Count; i++)
        {
            avgEloTeam1 += (float)team1[i].playerData.Elo;
        }
        avgEloTeam1 /= team1.Count;

        float avgEloTeam2 = 0;
        for (int i = 0; i < team2.Count; i++)
        {
            avgEloTeam2 += (float)team2[i].playerData.Elo;
        }
        avgEloTeam2 /= team2.Count;

        double expectedResultTeam1 = 1.0 / (1.0 + Math.Pow(10, (avgEloTeam2 - avgEloTeam1) / 400.0));
        double expectedResultTeam2 = 1.0 - expectedResultTeam1;

        int maxPool = CentralProperties.instance.totPools - 1;
        float maxK = 32f;
        float minK = 8f;

        //Debug.Log("Updating Elo Ratings...");
        // Elo update for team 1
        foreach (var p in team1)
        {
            p.playerData.GamesPlayed++;

            UpdateEloForPlayer(1, p, maxK, minK, maxPool, winner, expectedResultTeam1);

            CheckRankDerank(p);

            yield return null;
        }

        // Elo update for team 2
        foreach (var p in team2)
        {
            p.playerData.GamesPlayed++;

            UpdateEloForPlayer(2, p, maxK, minK, maxPool, winner, expectedResultTeam2);

            CheckRankDerank(p);

            yield return null;
        }

        UpdatePlayerStatusForBothTeams(ref team1, ref team2, false);

        Debug.LogWarning(matchSim + " Match Simulation Completed!");
    }

    void CheckRankDerank(Player p)
    {
        int currentPool = p.playerData.Pool;
        double elo = p.playerData.Elo;

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
            poolPlayersList[currentPool].UpdatePoolSize();
            poolPlayersList[newPool].playersInPool.Add(p);
            poolPlayersList[newPool].UpdatePoolSize();
            if(newPool > 0)
            {
                p.playerType = Player.PlayerType.Experienced;
            }
            p.playerData.Pool = newPool;

            p.poolHistory.Add(newPool);

            Debug.Log($"Player {p.playerData.Id} moved from Pool {currentPool} to Pool {newPool} (Elo: {elo})");
        }
    }

    void UpdateEloForPlayer(int team, Player p, float maxK, float minK, int maxPool, int winner, double expectedScore)
    {
        int poolIndex = p.playerData.Pool;
        float K = Mathf.Lerp(maxK, minK, poolIndex / (float)maxPool);
        double actualResult = winner == team ? 1.0 : 0.0;

        if (actualResult == 1.0) p.playerData.Wins++;

        double delta = K * (actualResult - expectedScore);
        p.playerData.Elo += delta;

        p.EloHistory.Add((float)p.playerData.Elo);
        p.totalChangeFromStart += (float)delta;

        //Debug.Log($"Team {team}\nPlayer {p.playerData.Id} (Pool {poolIndex}) Elo updated: {p.playerData.Elo} (Delta: {delta})");
    }

    public bool TrySplitFairTeamsAsync(List<Player> pool, ref List<Player> team1, ref List<Player> team2)
    {
        team1.Clear();
        team2.Clear();

        // Filter idle players only
        List<Player> idlePlayers = new();
        for(int i=0; i<pool.Count; i++)
        {
            if (pool[i].playerState == Player.PlayerState.Idle)
            {
                idlePlayers.Add(pool[i]);
            }
        }

        int totalRequired = teamSize * 2;
        if (idlePlayers.Count < totalRequired)
        {
            Debug.LogError($"Not enough IDLE players. Needed: {totalRequired}, Found: {idlePlayers.Count}");
            return false;
        }

        int maxAttempts = (int)((pool.Count * 0.2) + pool.Count);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            HashSet<int> selectedIndices = new();

            // Select 2 * teamSize unique indices randomly from the idlePlayers list
            while (selectedIndices.Count < totalRequired)
            {
                int randIndex = rng.Next(idlePlayers.Count);
                selectedIndices.Add(randIndex);
            }

            // Convert to list for indexing
            var selectedPlayers = selectedIndices.Select(i => idlePlayers[i]).ToList();

            // Shuffle the selected players (small list, so fast)
            for (int i = selectedPlayers.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (selectedPlayers[i], selectedPlayers[j]) = (selectedPlayers[j], selectedPlayers[i]);
            }

            // Split into two teams
            var t1 = selectedPlayers.Take(teamSize).ToList();
            var t2 = selectedPlayers.Skip(teamSize).Take(teamSize).ToList();

            float avgElo1 = t1.Average(p => (float)p.playerData.Elo);
            float avgElo2 = t2.Average(p => (float)p.playerData.Elo);

            if (Mathf.Abs(avgElo1 - avgElo2) <= eloThreshold)
            {
                team1 = t1;
                team2 = t2;

                UpdatePlayerStatusForBothTeams(ref team1, ref team2, true);

                if (logTeams)
                {
                    Debug.Log($"Fair match found! Elo diff: {Mathf.Abs(avgElo1 - avgElo2)}");
                    Debug.Log("Team 1: Elo: " + avgElo1);
                    Debug.Log("Team 2: Elo: " + avgElo2);
                }

                return true;
            }

            //// Let Unity breathe
            //if (attempt % 10 == 0)
            //    await Task.Yield();
        }

        Debug.LogWarning("No fair team found after 10,000 random samples.");
        return false;
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
