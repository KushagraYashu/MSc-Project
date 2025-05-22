using UnityEngine;

public class PlayerData : MonoBehaviour
{
    //id
    [SerializeField] private int _id;

    //elo
    [SerializeField] private double _elo;

    //composite skill
    [SerializeField] private double _compositeSkill = 0;

    //kd ratio
    private uint _kills = 0;
    private uint _deaths = 0;
    [SerializeField] private double _KD = 0;

    //assists and clutch ratios
    private uint _assists = 0;
    [SerializeField] private uint _assistRatio = 0;
    private uint _clutches = 0;
    [SerializeField] private uint _clutchRatio = 0;

    //experience
    [SerializeField] private uint _gamesPlayed = 0;
    private uint _wins = 0;

    //matching threshold
    private double _matchingThreshold = 0;

    [SerializeField] private bool _wantToPlay = false;

    public void SetPlayerData(int id, double baseElo, double matchingThreshold)
    {
        _id = id;
        _elo = baseElo;
        _matchingThreshold = matchingThreshold;
    }

    //getters and setters
    public int Id
    {
        get { return _id; }
        set { _id = value; }
    }

    public double Elo
    {
        get { return _elo; }
        set { _elo = value; }
    }

    public double KD
    {
        get { return _KD; }
    }

    public bool WantToPlay
    {
        get { return _wantToPlay; }
        set { _wantToPlay = value; }
    }

    public double CompositeSkill
    {
        get { return _compositeSkill; }
        set { _compositeSkill = value; }
    }
}
