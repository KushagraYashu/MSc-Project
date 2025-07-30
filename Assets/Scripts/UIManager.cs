using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    [SerializeField] private GameObject _firstScreenGO;
    [SerializeField] private GameObject _simulationScreenGO;
    [SerializeField] private GameObject _playerShowScreen;
    [SerializeField] private GameObject _sysNameTxtGO;

    [SerializeField] private GameObject _systemDropDownGO;
    [SerializeField] private GameObject _matchesPerPlayerInputFieldGO;

    [SerializeField] private GameObject[] _poolGOs;
    [SerializeField] private GameObject[] _poolPlayerCountTxtGOs;

    //internal variables
    int showPlayerIndex = 0;
    List<PlayerShowBox> allShowBoxes = new();
    
    public GameObject FirstScreen
    {
        get { return _firstScreenGO; }
    }

    public GameObject SimulationScreen
    {
        get { return _simulationScreenGO; }
    }

    public GameObject SysNameTxt
    {
        get { return _sysNameTxtGO; }
    }

    public GameObject MPPInputField
    {
        get { return _matchesPerPlayerInputFieldGO; }
    }

    public GameObject SystemDropDown
    {
        get { return _systemDropDownGO; }
    }

    public GameObject[] PoolPlayerCountTxtGOs
    {
        get { return _poolPlayerCountTxtGOs; }
    }

    List<Player> snapshot;
    int curPage = 0;
    public void ShowPool(int poolIndex)
    {
        _playerShowScreen.SetActive(true);

        var pool = EloSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
        snapshot = new List<Player>(pool.Where(p => p != null));

        snapshot = snapshot.OrderBy(p => p.playerData.Id).ToList();

        int count = Mathf.Min(allShowBoxes.Count, snapshot.Count);

        // Clear previous flags
        foreach (var box in allShowBoxes)
        {
            box.associatedPlayer = null;
            box.gameObject.SetActive(false);
        }

        for (int i = 0; i < count; i++)
        {
            var p = snapshot[i];
            allShowBoxes[i].associatedPlayer = p;
            allShowBoxes[i].gameObject.SetActive(true);
            allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";
            allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
        }

        curPage = 1;
    }

    public void LateUpdate()
    {
        foreach(PlayerShowBox box in allShowBoxes)
        {
            if (box.gameObject.activeInHierarchy)
            {
                if(box.associatedPlayer != null)
                {
                    box.eloTxtGO.GetComponent<TMPro.TMP_Text>().text = box.associatedPlayer.playerData.Elo.ToString("F4");
                }
            }
        }
    }

    public void NextPage()
    {
        int skip = curPage * allShowBoxes.Count;

        var thisPagePlayers = snapshot.Skip(skip).Take(allShowBoxes.Count).ToList();

        if(thisPagePlayers.Count > 0)
        {
            int count = Mathf.Min(allShowBoxes.Count, thisPagePlayers.Count);

            // Clear previous flags
            foreach (var box in allShowBoxes)
            {
                box.associatedPlayer = null;
                box.gameObject.SetActive(false);
            }

            for (int i = 0; i < count; i++)
            {
                var p = thisPagePlayers[i];
                allShowBoxes[i].associatedPlayer = p;
                allShowBoxes[i].gameObject.SetActive(true);
                allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";
                allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
            }

            curPage++;
        }
    }

    public void PrevPage()
    {
        if(curPage > 1)
        {
            int skip = curPage * allShowBoxes.Count;

            var thisPagePlayers = snapshot.Skip(skip).Take(allShowBoxes.Count).ToList();

            int count = Mathf.Min(allShowBoxes.Count, thisPagePlayers.Count);

            // Clear previous flags
            foreach (var box in allShowBoxes)
            {
                box.associatedPlayer = null;
                box.gameObject.SetActive(false);
            }

            for (int i = 0; i < count; i++)
            {
                var p = thisPagePlayers[i];
                allShowBoxes[i].associatedPlayer = p;
                allShowBoxes[i].gameObject.SetActive(true);
                allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";
                allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
            }

            curPage++;
        }
    }

    public void Print()
    {
        Debug.Log("Hello");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var firstBox = _playerShowScreen.GetComponentInChildren<PlayerShowBox>(false);
        if (firstBox != null)
        {
            allShowBoxes.Add(firstBox);
            PlayerShowBox currentBox = firstBox;
            while (currentBox.nextBox != null)
            {
                allShowBoxes.Add(currentBox.nextBox);
                currentBox = currentBox.nextBox;
            }
        }
        else
        {
            Debug.LogError("No PlayerShowBox found in _playerShowScreen!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
