using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Inactive,
        Playing
    }

    public enum PlayerType
    {
        Bot,
        Experienced,
        Newbie
    }

    [Header("Player Settings")]
    public PlayerState playerState;
    public PlayerType playerType;


    [Header("Visual Components")]
    public GameObject playerMesh;
    public Material[] playerMaterialsBasedOnState;

    [Header("Canvas Elements")]
    public TextMeshProUGUI IDTxt;
    public TextMeshProUGUI stateTxt;
    public TextMeshProUGUI typeTxt;
    public TextMeshProUGUI eloTxt;
    public TextMeshProUGUI CSTxt;
    public TextMeshProUGUI KDTxt;

    //internal variables
    PlayerData playerData;

    //Constructor
    public void SetPlayer(int id, double baseElo, double matchingThreshold, PlayerState state, PlayerType type)
    {
        playerState = state;
        playerType = type;
        playerMesh.GetComponent<MeshRenderer>().material = playerMaterialsBasedOnState[(int)state];

        //initialising player data
        if (playerData == null) playerData = GetComponent<PlayerData>();
        playerData.SetPlayerData(id, baseElo, matchingThreshold);

        UpdateCanvas();
    }

    public void UpdateCanvas()
    {
        IDTxt.text = "ID: " + playerData.Id.ToString();
        stateTxt.text = "State: " + playerState.ToString();
        typeTxt.text = "Type: " + playerType.ToString();

        eloTxt.text = "Elo: " + playerData.Elo.ToString();
        CSTxt.text = "CS: " + playerData.CompositeSkill.ToString();
        KDTxt.text = "KD: " + playerData.KD.ToString();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerData = GetComponent<PlayerData>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
