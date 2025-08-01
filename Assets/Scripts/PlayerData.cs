using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using Moserware.Skills;
using TrueSkill2;
using NUnit.Framework;
using System.Collections.Generic;

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

    //vanilla trueskill (moserware)
    [SerializeField] private Moserware.Skills.Rating _trueSkillRating = GameInfo.DefaultGameInfo.DefaultRating;

    //my trueskill
    [SerializeField] private TrueSkill2.Rating _myTrueSkillRating = new();

    //pool
    [SerializeField] private int _pool = 0;

    //composite skill
    [SerializeField] private double _compositeSkill = 0;
    private double _We = 1.00;
    private double _Wk = 12.500;
    private double _Wa = 5.500;
    private double _Wc = 9.000;
    private double _Wx = 0.50;

    //kd ratio
    private uint _kills = 0;
    private uint _deaths = 0;
    [SerializeField] private double _KDR = 0;

    //assists and clutch ratios
    private uint _assists = 0;
    [SerializeField] private double _assistRatio = 0;
    private uint _clutches = 0;
    private uint _clutchesPresented = 0;
    [SerializeField] private double _clutchRatio = 0;

    //experience
    [SerializeField] private uint _matchesToPlay = 0;
    [SerializeField] private uint _gamesPlayed = 0;
    private uint _roundsPlayed = 0;
    private uint _wins = 0;

    //history
    public List<int> Outcomes = new();

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

    public Moserware.Skills.Rating TrueSkillRating
    {
        get { return _trueSkillRating; }
        set { _trueSkillRating = value; }
    }
    
    public TrueSkill2.Rating MyTrueSkillRating
    {
        get { return _myTrueSkillRating; }
        set { _myTrueSkillRating = value; }
    }

    public int Pool
    {
        get { return _pool; }
        set { _pool = value; }
    }

    public uint Kills
    {
        get { return _kills; }
        set { _kills = value; }
    }

    public uint Assists
    {
        get { return _assists; }
        set { _assists = value; }
    }

    public uint Deaths
    {
        get { return _deaths; }
        set { _deaths = value; }
    }

    public double KDR
    {
        get { return _KDR; }
    }

    public uint Clutches
    {
        get { return _clutches; }
        set { _clutches = value; }
    }

    public uint ClutchesPresented
    {
        get { return _clutchesPresented; }
        set { _clutchesPresented = value; }
    }

    public bool WantToPlay
    {
        get { return _wantToPlay; }
        set { _wantToPlay = value; }
    }

    public double CompositeSkill
    {
        get { return _compositeSkill; }
    }

    public int GamesPlayed
    {
        get { return (int)_gamesPlayed; }
        set { _gamesPlayed = (uint)value; }
    }

    public int RoundsPlayed
    {
        get { return (int)_roundsPlayed; }
        set { _roundsPlayed = (uint)value; }
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

    public void UpdateKDR()
    {
        if (_deaths == 0)
        {
            _KDR = _kills;
        }
        else
        {
            _KDR = (_kills) / (double)_deaths;
        }
    }

    public void UpdateAssistRatio()
    {
        if(_roundsPlayed == 0)
        {
            _assistRatio = 0;
        }
        else
        {
            _assistRatio = _assists / (double)_roundsPlayed;
        }
    }

    public void UpdateClutchRatio()
    {
        if (_clutchesPresented == 0)
        {
            _clutchRatio = 0;
        }
        else
        {
            _clutchRatio = _clutches / (double)_clutchesPresented;
        }
    }

    public void UpdateCompositeSkill(int outcome)
    {
        var curCS = _compositeSkill;

        UpdateAssistRatio();
        UpdateClutchRatio();

        var newCS =
            _We * _elo +
            _Wk * _KDR +
            _Wa * _assistRatio +
            _Wc * _clutchRatio +
            _Wx * _gamesPlayed;

        if (outcome == 0)
        {
            //making sure composite skill always decreases (by at least 2) in a loss
            _compositeSkill = Mathf.Min((float)(curCS - 2), (float)newCS);
        }
        else
        {
            //limiting the composite skill increase to 100 (a player will require at least 4 games to rank up from a pool)
            _compositeSkill = Mathf.Min((float)newCS, (float)(curCS + 100));
        }
    }

    public float GetCompositeSkillCalculation()
    {
        return (float)(
            _We * _elo +
            _Wk * _KDR +
            _Wa * _assistRatio +
            _Wc * _clutchRatio +
            _Wx * _gamesPlayed
        );
    }

    public void CalculateAndAssignCompositeSkill()
    {
        UpdateAssistRatio();
        UpdateClutchRatio();

        _compositeSkill = 
            _We * _elo + 
            _Wk * _KDR + 
            _Wa * _assistRatio + 
            _Wc * _clutchRatio + 
            _Wx * _gamesPlayed
        ;
    }



    public float TrueSkillScaled(float minGlobal, float maxGlobal)
    {
        double conservative = _trueSkillRating.ConservativeRating;
        double normalised = (conservative - 0) / 50;
        double scaled = minGlobal + normalised * (maxGlobal - minGlobal);

        return (float)scaled;
    }


    const double defaultMu = 25;
    const double defaultSigma = 8.33333333; // mu / 3
    const double defaultConservativeRange = 50; //(0 to 50)
    public void ConvertToTrueSkill(
        float playerRating,
        float minPool,
        float maxPool,
        float minGlobal,
        float maxGlobal)
    {
        // 1. Normalize player rating to 0-1 range within pool
        double normalisedPoolRating = (playerRating - minPool) / (maxPool - minPool);

        // 2. Scale to global range (optional - only needed if pools are uneven)
        double normalisedGlobalRating = (minPool - minGlobal + (playerRating - minPool)) / (maxGlobal - minGlobal);

        // 3. Calculate target conservative rating (0-50)
        double targetConservative = normalisedGlobalRating * defaultConservativeRange;

        // 4. Solve for mu that satisfies: mu - 3sigma = targetConservative
        // Using default sigma for new players
        double targetMu = targetConservative + 3 * defaultSigma;

        _trueSkillRating = new Moserware.Skills.Rating(targetMu, defaultSigma);
    }
}
