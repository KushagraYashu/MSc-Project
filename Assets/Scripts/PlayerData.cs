using Moserware.Skills;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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

    //pool
    [SerializeField] private int _pool = 0;

    //composite skill
    [SerializeField] private double _compositeSkill = 0;
    private double _We = 01.00;
    private double _Wk = 10.00;
    private double _Wa = 04.00;
    private double _Wc = 07.00;
    private double _Wx = 00.25;
    private bool _limitExpPoints = true;
    private double _maxExpPoints = 200;
    private bool _limitCompositeSkill = true;

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

    //match specific data
    public int thisMatchKills = 0;
    public int thisMatchDeaths = 0;
    public int thisMatchAssists = 0;
    public int thisMatchRoundsPlayed = 0;
    public int thisMatchClutches = 0;
    public int thisMatchClutchesPresented = 0;
    public float thisMatchKDR = 0;
    public float thisMatchAssistRatio = 0;
    public float thisMatchClutchRatio = 0;

    //history
    public List<int> Outcomes = new();
    public List<float> PerformanceMultipliers = new();

    //matching threshold
    private double _matchingThreshold = 0;

    [SerializeField] private bool _wantToPlay = false;

    

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

    public double AssistRatio
    {
        get { return _assistRatio; }
    }

    public double ClutchRatio
    {
        get { return _clutchRatio; }
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

    //Elo
    public void SetPlayerData(int id, double baseElo, double realSkill, int pool, int matchesToPlay)
    {
        _id = id;
        _elo = baseElo;
        _realSkill = realSkill;
        _pool = pool;
        _matchesToPlay = (uint)matchesToPlay;
    }
    //Glicko
    public void SetPlayerData(int id, double baseElo, float RD, double realSkill, int pool, int matchesToPlay)
    {
        _id = id;
        _elo = baseElo;
        _rd = RD;
        _realSkill = realSkill;
        _pool = pool;
        _matchesToPlay = (uint)matchesToPlay;
    }
    //Smart System
    public void SetPlayerData(int id, double baseElo, double realSkill, int pool, int matchesToPlay, bool smartSys)
    {
        _id = id;
        _elo = baseElo;
        _rd = RD;
        _realSkill = realSkill;
        _pool = pool;
        _matchesToPlay = (uint)matchesToPlay;

        if (smartSys)
        {
            var weights = UIManager.instance.Weights;
            _We = weights.We;
            _Wa = weights.Wa;
            _Wk = weights.Wk;
            _Wc = weights.Wc;
            _Wx = weights.Wx;

            var expSettings = UIManager.instance.ExpSettings;
            _limitExpPoints = expSettings.LimitExp;
            _maxExpPoints = expSettings.MaxExpPoints;

            _limitCompositeSkill = UIManager.instance.LimitRatingPoints;

            CalculateAndAssignCompositeSkill();
        }
    }
    //TrueSkill
    public void SetPlayerData(int id, double baseElo, double realSkill, float trueskillRating, int pool, int matchesToPlay)
    {
        _id = id;
        _elo = baseElo;
        _rd = RD;
        _realSkill = realSkill;
        _pool = pool;
        _matchesToPlay = (uint)matchesToPlay;

        _trueSkillRating = new(trueskillRating, GameInfo.DefaultInitialStdDev);
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

    public void UpdateCompositeSkillAndElo(double delta, int outcome)
    {
        var minElo = CentralProperties.instance.eloRangePerPool[0].x;
        var maxElo = CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y;

        //Elo update
        if (_limitCompositeSkill)
            _elo = Mathf.Clamp((float)_elo + (float)delta, minElo, maxElo);
        else
            _elo += delta;

        UpdateAssistRatio();
        UpdateClutchRatio();
        UpdateKDR();

        //CS update
        var curCS = _compositeSkill;
        var newCS =
            _We * _elo +
            _Wk * _KDR +
            _Wa * _assistRatio +
            _Wc * _clutchRatio +
            (_limitExpPoints == true 
                ? Mathf.Min((float)_Wx * _gamesPlayed, (float)_maxExpPoints)
                : _Wx * _gamesPlayed);

        _compositeSkill = newCS;
        if (outcome == 0)
        {
            //making sure composite skill always decreases (by at least 2) in a loss, and max by 100
            _compositeSkill = Mathf.Clamp((float)_compositeSkill, (float)curCS - 100, (float)curCS - 2);
        }
        else
        {
            //limiting the composite skill increase to 100 and a minimum of 2 (a player will require at least 4 games to rank up from a pool)
            _compositeSkill = Mathf.Clamp((float)_compositeSkill, (float)curCS + 2, (float)curCS + 100);
        }

        if (_limitCompositeSkill)
            _compositeSkill = Mathf.Clamp((float)_compositeSkill, minElo, maxElo);
    }

    public float GetCompositeSkillCalculation()
    {
        UpdateKDR();
        UpdateAssistRatio();
        UpdateClutchRatio();

        return (float)(
            _We * _elo +
            _Wk * _KDR +
            _Wa * _assistRatio +
            _Wc * _clutchRatio +
            (_limitExpPoints == true
                ? Mathf.Min((float)_Wx * _gamesPlayed, (float)_maxExpPoints)
                : _Wx * _gamesPlayed)
        );
    }

    public void CalculateAndAssignCompositeSkill()
    {
        UpdateAssistRatio();
        UpdateClutchRatio();
        UpdateKDR();

        _compositeSkill = 
            _We * _elo + 
            _Wk * _KDR + 
            _Wa * _assistRatio + 
            _Wc * _clutchRatio +
            (_limitExpPoints == true
                ? Mathf.Min((float)_Wx * _gamesPlayed, (float)_maxExpPoints)
                : _Wx * _gamesPlayed)
        ;
    }

    public void ResetMatchData()
    {
        thisMatchKills = 0;
        thisMatchDeaths = 0;
        thisMatchAssists = 0;
        thisMatchRoundsPlayed = 0;
        thisMatchClutches = 0;
        thisMatchClutchesPresented = 0;
        thisMatchKDR = 0;
        thisMatchAssistRatio = 0;
        thisMatchClutchRatio = 0;
    }

    public void CalculateMatchData()
    {
        //KDR
        if (thisMatchDeaths == 0)
        {
            thisMatchKDR = thisMatchKills;
        }
        else
        {
            thisMatchKDR = (float)thisMatchKills / thisMatchDeaths;
        }

        //assist ratio
        if (thisMatchRoundsPlayed == 0)
        {
            thisMatchAssistRatio = 0;
        }
        else
        {
            thisMatchAssistRatio = (float)thisMatchAssists / thisMatchRoundsPlayed;
        }

        //clutch ratio
        if (thisMatchClutchesPresented == 0)
        {
            thisMatchClutchRatio = 0;
        }
        else
        {
            thisMatchClutchRatio = (float)thisMatchClutches / thisMatchClutchesPresented;
        }
    }

    public float CalculateMatchPerformance()
    {
        float KDRratio = 0;
        float assistRatio = 0;
        float clutchRatio = 0;
        if (_KDR != 0)
            KDRratio = (float)(thisMatchKDR / _KDR);
        else
            KDRratio = thisMatchKDR;
        if (_assistRatio != 0)
            assistRatio = (float)(thisMatchAssistRatio / _assistRatio);
        else
            assistRatio = thisMatchAssistRatio;
        if (_clutchRatio != 0)
            clutchRatio = (float)(thisMatchClutchRatio / _clutchRatio);
        else
            clutchRatio = thisMatchClutchRatio;

        return Mathf.Clamp(KDRratio + assistRatio + clutchRatio, 0.8f, 2f);
    }
}
