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
        Bot,
        Experienced,
        Newbie,
        TOTAL
    }

    [Header("Player Settings")]
    public PlayerState playerState;
    public PlayerType playerType;

    [Header("Pool")]
    public List<int> poolHistory = new();

    [Header("Delta")]
    public float totalChangeFromStart = 0f;

    [Header("Glicko Debug things")]
    public List<float> RDHistory = new();
    public List<float> EloHistory = new();

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
    public void SetPlayer(int id, double baseElo, int pool, double matchingThreshold, PlayerState state, PlayerType type)
    {
        playerState = state;
        playerType = type;
        //playerMesh.GetComponent<MeshRenderer>().material = playerMaterialsBasedOnState[(int)state];

        //initialising player data
        if (playerData == null) playerData = new();
        playerData.SetPlayerData(id, baseElo, pool, matchingThreshold);

        //UpdateCanvas();
    }

    public void PrintData()
    {
        Debug.Log("ID: " + playerData.Id + 
            "\tState: " + playerState + 
            "\tType: " + playerType + 
            "\tElo: " + playerData.Elo);
        
    }
}
