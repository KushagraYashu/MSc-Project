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
    PlayerDetailsPanel _currentActiveDetailsPanel = null;

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
        if(_currentActiveDetailsPanel != null)
        {
            _currentActiveDetailsPanel.gameObject.SetActive(false);
            _currentActiveDetailsPanel = null;
        }
        _playerShowScreen.SetActive(true);

        List<Player> pool = new();

        switch (MainServer.instance.SystemIndex)
        {
            case 0: //Elo
                pool = EloSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                break;

            case 1: //glicko
                pool = GlickoSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                break;

            case 2: //vanilla trueskill (moserware)
                pool = VanillaTrueskillSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                break;

            case 3: //smart match
                pool = SmartMatchSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                break;
        }

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

            if (p.playerType == Player.PlayerType.Smurf)
                allShowBoxes[i].bgImage.color = new Color(1f, 1f, 0f, allShowBoxes[i].bgImage.color.a);
            else
                allShowBoxes[i].bgImage.color = new Color(1f, 1f, 1f, allShowBoxes[i].bgImage.color.a);

            allShowBoxes[i].gameObject.SetActive(true);
            allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";

            switch (MainServer.instance.SystemIndex)
            {
                case 0: //elo
                    allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                    break;

                case 1: //glicko
                    allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                    break;

                case 2: //vanilla trueskill (moserware)
                    allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.TrueSkillScaled(CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y).ToString("F4");
                    break;

                case 3: //smart match
                    allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.CompositeSkill.ToString("F4");
                    break;
            }
        }

        curPage = 1;
    }

    

    public void NextPage()
    {
        if (_currentActiveDetailsPanel != null)
        {
            _currentActiveDetailsPanel.gameObject.SetActive(false);
            _currentActiveDetailsPanel = null;
        }

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

                if (p.playerType == Player.PlayerType.Smurf)
                    allShowBoxes[i].bgImage.color = new Color(1f, 1f, 0f, allShowBoxes[i].bgImage.color.a);
                else
                    allShowBoxes[i].bgImage.color = new Color(1f, 1f, 1f, allShowBoxes[i].bgImage.color.a);

                allShowBoxes[i].gameObject.SetActive(true);
                allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";

                switch (MainServer.instance.SystemIndex)
                {
                    case 0: //elo
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 1: //glicko
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 2: //vanilla trueskill (moserware)
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.TrueSkillScaled(CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y).ToString("F4");
                        break;

                    case 3: //smart match
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.CompositeSkill.ToString("F4");
                        break;
                }
            }

            curPage++;
        }
    }

    public void PrevPage()
    {
        if (_currentActiveDetailsPanel != null)
        {
            _currentActiveDetailsPanel.gameObject.SetActive(false);
            _currentActiveDetailsPanel = null;
        }

        if (curPage > 1)
        {
            curPage = Mathf.Max(0, curPage - 2);

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

                if (p.playerType == Player.PlayerType.Smurf)
                    allShowBoxes[i].bgImage.color = new Color(1f, 1f, 0f, allShowBoxes[i].bgImage.color.a);
                else
                    allShowBoxes[i].bgImage.color = new Color(1f, 1f, 1f, allShowBoxes[i].bgImage.color.a);

                allShowBoxes[i].gameObject.SetActive(true);
                allShowBoxes[i].IDTxtGO.GetComponent<TMPro.TMP_Text>().text = $"Player ID: {p.playerData.Id}";

                switch (MainServer.instance.SystemIndex)
                {
                    case 0: //elo
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 1: //glicko
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 2: //vanilla trueskill (moserware)
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.TrueSkillScaled(CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y).ToString("F4");
                        break;

                    case 3: //smart match
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.CompositeSkill.ToString("F4");
                        break;
                }
            }

            curPage++;
        }
    }

    
    public void ShowPlayerDetails(GameObject showBox)
    {
        if (_currentActiveDetailsPanel != null)
        {
            _currentActiveDetailsPanel.gameObject.SetActive(false);

            if(_currentActiveDetailsPanel == showBox.GetComponent<PlayerShowBox>().detailsPanel)
            {
                _currentActiveDetailsPanel = null;
                return;
            }
        }

        var ShowBox = showBox.GetComponent<PlayerShowBox>();
        _currentActiveDetailsPanel = ShowBox.detailsPanel;

        _currentActiveDetailsPanel.GetComponent<PlayerDetailsPanel>().ShowDetails(ShowBox.associatedPlayer);

        _currentActiveDetailsPanel.gameObject.SetActive(true);
    }

    public void UpdateBoxContent(Player p)
    {
        foreach(var box in allShowBoxes)
        {
            if(p == box.associatedPlayer && box.gameObject.activeInHierarchy)
            {
                box.UpdateDetails();
            }
        }
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
