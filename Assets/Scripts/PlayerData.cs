using UnityEngine;

[System.Serializable]
public class PlayerData
{
    //id
    [SerializeField] private int _id;

    //elo
    [SerializeField] private double _elo;

    //real skill
    [SerializeField] private double _realSkill = 0;

    //glicko
    [SerializeField] private double _rd = 350f;

    //pool
    [SerializeField] private int _pool = 0;

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
    [SerializeField] private uint _matchesToPlay = 0;
    [SerializeField] private uint _gamesPlayed = 0;
    private uint _wins = 0;

    //matching threshold
    private double _matchingThreshold = 0;

    [SerializeField] private bool _wantToPlay = false;

    public void SetPlayerData(int id, double baseElo, double realSkill, int pool, double matchingThreshold)
    {
        _id = id;
        _elo = baseElo;
        _realSkill = realSkill;
        _matchingThreshold = matchingThreshold;
        _pool = pool;
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

    public double RealSkill
    {
        get { return _realSkill; }
        set { _realSkill = value; }
    }

    public double RD
    {
        get { return _rd; }
        set { _rd = value; }
    }

    public int Pool
    {
        get { return _pool; }
        set { _pool = value; }
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

    public int GamesPlayed
    {
        get { return (int)_gamesPlayed; }
        set { _gamesPlayed = (uint)value; }
    }

    public int MatchesToPlay
    {
        get { return (int)_matchesToPlay; }
        set { _matchesToPlay = (uint)value; }
    }

    public int Wins
    {
        get { return (int)_wins; }
        set { _wins = (uint)value; }
    }
}
