using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class EloSystemManager : MonoBehaviour
{
    public static EloSystemManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    [Header("TOTAL MATCHES")]
    public int totalMatches = 1000;

    [Header("Match Properties")]
    public int whichPool = 0;
    public int maxRoundsPerMatch;
    public int teamSize = 5;
    public float eloThreshold = 10f;

    [Header("Teams")]
    public List<Player> team1 = new();
    public List<Player> team2 = new();

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
        for (int i = 0; i < cp.totPools; i++)
        {
            float minElo = cp.eloRangePerPool[i].x;
            float maxElo = cp.eloRangePerPool[i].y;

            for (int j = 0; j < poolPlayers[i]; j++)
            {
                Player newPlayer = new();
                newPlayer.SetPlayer(MainServer.instance.GenerateRandomID(maxAttempts, maxIDs),
                                    UnityEngine.Random.Range(minElo, maxElo),
                                    i,
                                    eloThreshold,
                                    Player.PlayerState.Idle,
                                    (i > 0) ? Player.PlayerType.Experienced : Player.PlayerType.Newbie);

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
        StartCoroutine(SimulateForMatches());
    }

    IEnumerator SimulateForMatches()
    {
        for(int i = 0; i < totalMatches; i++)
        {
            whichPool = UnityEngine.Random.Range(0, CentralProperties.instance.totPools);
            bool done = false;

            StartTeamSplit(poolPlayersList[whichPool].playersInPool, () =>
            {
                done = true;
            });

            while (!done) yield return null;

            yield return null;
        }
    }

    public async void StartTeamSplit(List<Player> playerPool, Action onMatchDone)
    {
        playerPool = playerPool.OrderBy(p => UnityEngine.Random.value).ToList();

        bool teamsCreated = await TrySplitFairTeamsAsync(playerPool);

        if ((teamsCreated))
        {
            Debug.Log("Do the Match");
            StartCoroutine(SimulateMatch(onMatchDone));
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

            StartTeamSplit(combinedPool, onMatchDone);
        }
    }

    void UpdatePlayerStatusForBothTeams(bool playing = false)
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

    IEnumerator SimulateMatch(Action onComplete)
    {
        UpdatePlayerStatusForBothTeams(true);

        int team1wins = 0;
        int team2wins = 0;

        System.Random rng = new();

        for(int round = 0; round < maxRoundsPerMatch; round++)
        {
            List<Player> team1Shuffled = team1.OrderBy(x => rng.Next()).ToList();
            List<Player> team2Shuffled = team2.OrderBy(x => rng.Next()).ToList();

            int team1Score = 0;
            int team2Score = 0;

            //1v1s
            for(int i=0; i<team1.Count; i++)
            {
                Player p1 = team1Shuffled[i];
                Player p2 = team2Shuffled[i];

                double p1WinProb = 1.0 / (1.0 + Math.Pow(10, (p2.playerData.Elo - p1.playerData.Elo) / 400.0));

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

                yield return null;
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

            Debug.Log($"Round {round + 1}: Team 1: {team1Score}, Team 2: {team2Score}");

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

        float avgEloTeam1 = team1.Average(p => (float)p.playerData.Elo);
        float avgEloTeam2 = team2.Average(p => (float)p.playerData.Elo);

        double expectedScoreTeam1 = 1.0 / (1.0 + Math.Pow(10, (avgEloTeam2 - avgEloTeam1) / 400.0));
        double expectedScoreTeam2 = 1.0 - expectedScoreTeam1;

        int maxPool = CentralProperties.instance.totPools - 1;
        float maxK = 32f;
        float minK = 8f;

        Debug.Log("Updating Elo Ratings...");
        // Elo update for team 1
        foreach (var p in team1)
        {
            UpdateEloForPlayer(1, p, maxK, minK, maxPool, winner, expectedScoreTeam1);

            CheckRankDerank(p);

            yield return null;
        }

        // Elo update for team 2
        foreach (var p in team2)
        {
            UpdateEloForPlayer(2, p, maxK, minK, maxPool, winner, expectedScoreTeam2);

            CheckRankDerank(p);

            yield return null;
        }

        UpdatePlayerStatusForBothTeams(false);

        team1.Clear();
        team2.Clear();

        onComplete?.Invoke();
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
            p.playerData.Pool = newPool;

            Debug.Log($"Player {p.playerData.Id} moved from Pool {currentPool} to Pool {newPool} (Elo: {elo})");
        }
    }

    void UpdateEloForPlayer(int team, Player p, float maxK, float minK, int maxPool, int winner, double expectedScore)
    {
        int poolIndex = p.playerData.Pool;
        float K = Mathf.Lerp(maxK, minK, poolIndex / (float)maxPool);
        double actual = winner == team ? 1.0 : 0.0;
        double delta = K * (actual - expectedScore);
        p.playerData.Elo += delta;
        Debug.Log($"Team {team}\nPlayer {p.playerData.Id} (Pool {poolIndex}) Elo updated: {p.playerData.Elo} (Delta: {delta})");
    }

    public async Task<bool> TrySplitFairTeamsAsync(List<Player> pool)
    {
        team1.Clear();
        team2.Clear();

        // Filter idle players only
        List<Player> idlePlayers = pool.Where(p => p.playerState == Player.PlayerState.Idle).ToList();

        int totalRequired = teamSize * 2;
        if (idlePlayers.Count < totalRequired)
        {
            Debug.LogError($"Not enough IDLE players. Needed: {totalRequired}, Found: {idlePlayers.Count}");
            return false;
        }

        System.Random rng = new();

        int maxAttempts = 10000;

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

                if (logTeams)
                {
                    Debug.Log($"Fair match found! Elo diff: {Mathf.Abs(avgElo1 - avgElo2)}");
                    Debug.Log("Team 1: Elo: " + avgElo1 + "\n" + string.Join(", ", team1.Select(p => $"{p.playerData.Id} ({p.playerData.Elo})")));
                    Debug.Log("Team 2: Elo: " + avgElo2 + "\n" + string.Join(", ", team2.Select(p => $"{p.playerData.Id} ({p.playerData.Elo})")));
                }

                return true;
            }

            // Let Unity breathe
            if (attempt % 10 == 0)
                await Task.Yield();
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
