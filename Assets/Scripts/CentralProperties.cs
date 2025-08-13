using UnityEngine;

public class CentralProperties : MonoBehaviour
{
    public static CentralProperties instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }


    public int totPlayers;

    public int totSmurfs;

    public int totPools = 4;

    public Vector2[] eloRangePerPool;

    public float[] playerDistributionInPools;

    public float minGlobalRating;

    public float maxGlobalRating;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        totSmurfs = Mathf.CeilToInt(0.01f * totPlayers);  //1 percent of players are smurfs

        minGlobalRating = eloRangePerPool[0].x;
        maxGlobalRating = eloRangePerPool[totPools - 1].y;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
