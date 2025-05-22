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

    [Header("Spawn System")]
    public float spacing;
    public Transform startPosition;
    public Transform columnEndPosition;
    public Transform rowEndPosition;
    [SerializeField]int rows;
    [SerializeField]int columns;
    [SerializeField] int totSpawn = 0;

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
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = new Vector3(
                    startPosition.position.x + col * spacing,
                    1,
                    startPosition.position.z + row * spacing
                );

                totSpawn++;
                GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                player.name = $"Player_{row}_{col}";
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetupSystem();
        }
    }
}
