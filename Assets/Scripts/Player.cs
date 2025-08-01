using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class Player
{
    public enum PlayerState
    {
        Idle,
        Playing
    }

    public enum PlayerType
    {
        Smurf,
        Experienced,
        Newbie,
        TOTAL
    }

    public enum PlayerPlayStyle
    {
        Basic,
        Fragger,
        Support,
        Clutcher,
        TOTAL,
    }

    [Header("Player Settings")]
    public PlayerState playerState;
    public PlayerType playerType = PlayerType.TOTAL;
    public List<PlayerPlayStyle> playerPlayStyles = new();

    [Header("Pool")]
    public List<int> poolHistory = new();

    [Header("Delta")]
    public float totalChangeFromStart = 0f;

    [Header("Glicko Debug things")]
    public List<float> RDHistory = new();
    public List<float> EloHistory = new();

    [Header("Trueskill debug things")]
    public List<double> muHistory = new();
    public List<double> sigmaHistory = new();
    public List<double> conservativeValHistory = new();
    public List<double> scaledRatingHistory = new();

    [Header("Custom TrueSkill debug things")]
    public List<double> scaledMuHistory = new();
    public List<double> scaledSigmaHistory = new();
    public List<double> scaledConservativeValHistory = new();


    //[Header("Visual Components")]
    //public GameObject playerMesh;
    //public Material[] playerMaterialsBasedOnState;

    //[Header("Canvas Elements")]
    //public TextMeshProUGUI IDTxt;
    //public TextMeshProUGUI stateTxt;
    //public TextMeshProUGUI typeTxt;
    //public TextMeshProUGUI eloTxt;
    //public TextMeshProUGUI CSTxt;
    //public TextMeshProUGUI KDTxt;

    [SerializeField]
    public PlayerData playerData;

    //Constructor
    public void SetPlayer(int id, double baseElo, double realSkill,int pool, double matchingThreshold, PlayerState state)
    {
        playerState = state;
        if (playerType == PlayerType.TOTAL)
        {
            if (pool > 0)
                playerType = PlayerType.Experienced;
            else
                playerType = PlayerType.Newbie;
        }
        //playerMesh.GetComponent<MeshRenderer>().material = playerMaterialsBasedOnState[(int)state];

        //initialising player data
        playerData ??= new();
        playerData.SetPlayerData(id, baseElo, realSkill, pool, matchingThreshold);

        //UpdateCanvas();
    }

    public void PrintData()
    {
        Debug.Log("ID: " + playerData.Id + 
            "\tState: " + playerState + 
            "\tType: " + playerType + 
            "\tElo: " + playerData.Elo);
        
    }

    public bool IsOnLosingStreak(List<int> outcomes)
    {
        if (outcomes.Count < 5) return false;

        int count = 0;
        for (int i = outcomes.Count - 1; i >= 0; i--)
        {
            if (outcomes[i] == 0)
            {
                count++;
                if (count >= 5)
                    return true;
            }
            else break;
        }

        return false;
    }
}
