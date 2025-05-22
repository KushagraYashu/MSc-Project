using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainServer : MonoBehaviour
{
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
    public Player.PlayerState playerState = Player.PlayerState.Inactive;

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
    int rows;
    int columns;
    int totSpawn = 0;
    int totExpSpawn = 0;
    int totNewbieSpawn = 0;
    int totBotSpawn = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CalculateSpawningGrid();
    }

    void CalculateSpawningGrid()
    {
        rows = Mathf.FloorToInt(Vector3.Distance(rowEndPosition.position, startPosition.position) / spacing);
        columns = Mathf.FloorToInt(Vector3.Distance(columnEndPosition.position, startPosition.position) / spacing);

    }

    //Setup the system
    public void SetupSystem()
    {
        StartCoroutine(SpawnPlayers());
    }

    IEnumerator SpawnPlayers()
    {
        totPlayerCount = int.Parse(totPlayerCount_IF.text);
        expPlayerCount = int.Parse(expPlayerCount_IF.text);
        newbiePlayerCount = totPlayerCount - expPlayerCount;
        botPlayerCount = Mathf.FloorToInt(.3f * totPlayerCount);
        totPlayerCount += botPlayerCount;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if(totSpawn >= totPlayerCount)
                {
                    yield break;
                }

                Vector3 spawnPos = new Vector3(
                    startPosition.position.x + col * spacing,
                    1,
                    startPosition.position.z + row * spacing
                );

                if(totExpSpawn < expPlayerCount)
                {
                    GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                    int id = GenerateRandomID();
                    if(id != -1)
                    {
                        player.GetComponent<Player>().SetPlayer(id, baseElo, matchingThreshold, playerState, Player.PlayerType.Experienced);
                        player.name = "Player_" + id.ToString();
                    }
                    players.Add(player);

                    totExpSpawn++;
                }

                else if(totNewbieSpawn < newbiePlayerCount)
                {
                    GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                    int id = GenerateRandomID();
                    if (id != -1)
                    {
                        player.GetComponent<Player>().SetPlayer(id, baseElo, matchingThreshold, playerState, Player.PlayerType.Newbie);
                        player.name = "Player_" + id.ToString();
                    }
                    newbies.Add(player);

                    totNewbieSpawn++;
                }

                else if(totBotSpawn < botPlayerCount)
                {
                    GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                    int id = GenerateRandomID();
                    if (id != -1)
                    {
                        player.GetComponent<Player>().SetPlayer(id, baseElo, matchingThreshold, Player.PlayerState.Idle, Player.PlayerType.Bot);
                        player.name = "Bot_" + id.ToString();
                    }
                    bots.Add(player);

                    totBotSpawn++;
                }

                totSpawn++;
                yield return null;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    readonly int maxAttempts = 12000;       //giving the attemps some overhead (2k)
    private HashSet<int> allIDs = new();
    int GenerateRandomID()
    {
        for (int attempt = 0; attempt < maxAttempts; ++attempt)
        {
            int id = UnityEngine.Random.Range(0, 10000);  // 7k players, 3k bots
            if (allIDs.Add(id)) // HashSet.Add returns false if already present
            {
                return id;
            }
        }

        Debug.LogError("No available IDs left.");
        return -1;
    }
}
