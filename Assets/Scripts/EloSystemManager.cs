using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;

public class EloSystemManager : MonoBehaviour
{
    public static EloSystemManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    [Header("Match Properties")]
    public int maxRoundsPerMatch;
    public int teamSize = 5;
    public int eloThreshold = 10;
    public List<Player> team1 = new();
    public List<Player> team2 = new();

    [Header("Debug Info")]
    public bool logTeams = true;

    [SerializeField]
    public List<Player> players = new();

    [System.Serializable]
    public class PoolPlayers
    {
        public List<Player> playersInPool;
        public PoolPlayers()
        {
            playersInPool = new List<Player>();
        }
    };
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
        
        int[] poolPlayers = new int[CentralProperties.instance.totPools];
        for(int i=0; i < CentralProperties.instance.totPools; i++)
        {
            poolPlayers[i] = Mathf.FloorToInt(CentralProperties.instance.totPlayers * CentralProperties.instance.playerDistributionInPools[i]/100);

            yield return null;
        }

        poolPlayersList = new PoolPlayers[CentralProperties.instance.totPools];
        for (int i = 0; i < poolPlayersList.Length; i++)
        {
            poolPlayersList[i] = new PoolPlayers();
        }
        for (int i = 0; i < CentralProperties.instance.totPools; i++)
        {
            for (int j = 0; j < poolPlayers[i]; j++)
            {
                Player newPlayer = new();
                newPlayer.SetPlayer(MainServer.instance.GenerateRandomID(),
                                    Random.Range(CentralProperties.instance.eloRangePerPool[i].x, CentralProperties.instance.eloRangePerPool[i].y),
                                    eloThreshold,
                                    Player.PlayerState.Idle,
                                    (i > 0)? Player.PlayerType.Experienced : Player.PlayerType.Newbie);

                players.Add(newPlayer);

                poolPlayersList[i].playersInPool.Add(newPlayer);

                //newPlayer.PrintData();
            }
        }
    }

    public void CreateAMatch()
    {
        StartTeamSplit(poolPlayersList[0].playersInPool);
    }

    public void StartTeamSplit(List<Player> playerPool)
    {
        StartCoroutine(TrySplitFairTeams(playerPool));
    }

    public IEnumerator TrySplitFairTeams(List<Player> pool)
    {
        team1.Clear();
        team2.Clear();

        int totalRequired = teamSize * 2;
        if (pool.Count < totalRequired)
        {
            Debug.LogError($"Not enough players in pool. Needed: {totalRequired}, Found: {pool.Count}");
            yield break;
        }

        List<List<Player>> team1Combos = GenerateCombinations(pool, teamSize);

        for (int i = 0; i < team1Combos.Count; i++)
        {
            List<Player> t1 = team1Combos[i];

            // Create remaining list for team2 candidates
            List<Player> remaining = new List<Player>(pool);
            foreach (var p in t1) remaining.Remove(p);

            List<List<Player>> team2Combos = GenerateCombinations(remaining, teamSize);

            for (int j = 0; j < team2Combos.Count; j++)
            {
                List<Player> t2 = team2Combos[j];

                float elo1 = t1.Sum(p => (float)p.playerData.Elo);
                elo1 /= teamSize;
                float elo2 = t2.Sum(p => (float)p.playerData.Elo);
                elo2 /= teamSize;

                if (Mathf.Abs(elo1 - elo2) <= eloThreshold)
                {
                    team1 = new List<Player>(t1);
                    team2 = new List<Player>(t2);

                    if (logTeams)
                    {
                        Debug.Log($"Fair match found! Elo Diff: {Mathf.Abs(elo1 - elo2)}");
                        Debug.Log("Team 1: Team Elo: " + elo1 + "\n" + string.Join(", ", team1.Select(p => $"{p.playerData.Id} ({p.playerData.Elo})")));
                        Debug.Log("Team 2: Team Elo: " + elo2 + "\n" + string.Join(", ", team2.Select(p => $"{p.playerData.Id} ({p.playerData.Elo})")));
                    }

                    yield break;
                }

                // Yield occasionally to keep Unity responsive
                if (j % 10 == 0)
                    yield return null;
            }

            if (i % 10 == 0)
                yield return null;
        }

        Debug.LogWarning("Could not find fair teams from this pool.");
    }

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
