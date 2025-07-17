using UnityEngine;

public class CentralProperties : MonoBehaviour
{
    public static CentralProperties instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }


    public uint totPlayers;

    public int totSmurfs;

    public int totPools = 4;

    public Vector2[] eloRangePerPool;

    public float[] playerDistributionInPools;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
