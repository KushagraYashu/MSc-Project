using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private GameObject _addPlayerScreenGO;
    [SerializeField] private GameObject _newPlayerRatingGO;
    [SerializeField] private GameObject _newPlayerSmurfCheckboxGO;

    [SerializeField] private GameObject _smartSystemConfigurablesScreenGO;
    [SerializeField] private GameObject[] _weightInputFieldsGOs;
    [SerializeField] private GameObject _limitExpCheckboxGO;
    [SerializeField] private GameObject _limitExpInputFieldGO;
    [SerializeField] private GameObject _limitRatingPointsCheckboxGO;

    //internal variables
    int showPlayerIndex = 0;
    List<PlayerShowBox> allShowBoxes = new();
    PlayerDetailsPanel _currentActiveDetailsPanel = null;

    private double _We = 01.00;
    private double _Wk = 10.00;
    private double _Wa = 04.00;
    private double _Wc = 07.00;
    private double _Wx = 00.25;

    private float _maxExpPoints = 200;
    private bool _limitExp = true;
    private bool _limitRatingPoints = true;

    public (double We, double Wk, double Wa, double Wc, double Wx) Weights
    {
        get { return (_We, _Wk, _Wa, _Wc, _Wx); }
    }

    public (bool LimitExp, float MaxExpPoints) ExpSettings
    {
        get { return (_limitExp, _maxExpPoints); }
    }

    public bool LimitRatingPoints
    {
        get { return _limitRatingPoints; }
    }

    public void UpdateSmartSystemConfigurations()
    {
        _We = double.Parse(_weightInputFieldsGOs[0].GetComponent<TMP_InputField>().text);
        _Wk = double.Parse(_weightInputFieldsGOs[1].GetComponent<TMP_InputField>().text);
        _Wa = double.Parse(_weightInputFieldsGOs[2].GetComponent<TMP_InputField>().text);
        _Wc = double.Parse(_weightInputFieldsGOs[3].GetComponent<TMP_InputField>().text);
        _Wx = double.Parse(_weightInputFieldsGOs[4].GetComponent<TMP_InputField>().text);

        _limitExp = _limitExpCheckboxGO.GetComponent<Toggle>().isOn;
        if (_limitExp)
        {
            _maxExpPoints = float.Parse(_limitExpInputFieldGO.GetComponent<TMP_InputField>().text);
        }
        else
        {
            _maxExpPoints = 0;
        }

        _limitRatingPoints = _limitRatingPointsCheckboxGO.GetComponent<Toggle>().isOn;
    }

    public void UpdateUIForSmartSystemConfigurations()
    {
        _weightInputFieldsGOs[0].GetComponent<TMP_InputField>().text = _We.ToString("F4");
        _weightInputFieldsGOs[1].GetComponent<TMP_InputField>().text = _Wk.ToString("F4");
        _weightInputFieldsGOs[2].GetComponent<TMP_InputField>().text = _Wa.ToString("F4");
        _weightInputFieldsGOs[3].GetComponent<TMP_InputField>().text = _Wc.ToString("F4");
        _weightInputFieldsGOs[4].GetComponent<TMP_InputField>().text = _Wx.ToString("F4");

        _limitExpCheckboxGO.GetComponent<Toggle>().isOn = _limitExp;
        _limitExpInputFieldGO.GetComponent<TMP_InputField>().text = _maxExpPoints.ToString("F4");
        _limitRatingPointsCheckboxGO.GetComponent<Toggle>().isOn = _limitRatingPoints;
    }

    public void CheckSystem(int system)
    {
        if(system == 3)
        {
            UpdateUIForSmartSystemConfigurations();
            _smartSystemConfigurablesScreenGO.SetActive(true);
        }
    }

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

    public GameObject NewPlayerRating
    {
        get { return _newPlayerRatingGO; }
    }

    public GameObject NewPlayerSmurfCheckbox
    {
        get { return _newPlayerSmurfCheckboxGO; }
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
                snapshot = new List<Player>(pool.Where(p => p != null));
                snapshot = snapshot.OrderBy(p => p.playerData.Id).ThenBy(p => p.playerData.Elo).ToList();
                break;

            case 1: //glicko
                pool = GlickoSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                snapshot = new List<Player>(pool.Where(p => p != null));
                snapshot = snapshot.OrderBy(p => p.playerData.Id).ThenBy(p => p.playerData.Elo).ToList();
                break;

            case 2: //vanilla trueskill (moserware)
                pool = VanillaTrueskillSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                snapshot = new List<Player>(pool.Where(p => p != null));
                snapshot = snapshot.OrderBy(p => p.playerData.Id).ThenBy(p => VanillaTrueskillSystemManager.instance.ConvertRating((float)p.playerData.TrueSkillRating.Mean, CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y, VanillaTrueskillSystemManager.RatingConversion.To_MyRating)).ToList();
                break;

            case 3: //smart match
                pool = SmartMatchSystemManager.instance.poolPlayersList[poolIndex].playersInPool;
                snapshot = new List<Player>(pool.Where(p => p != null));
                snapshot = snapshot.OrderBy(p => p.playerData.Id).ThenBy(p => p.playerData.CompositeSkill).ToList();
                break;
        }

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
                    allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = VanillaTrueskillSystemManager.instance.ConvertRating((float)p.playerData.TrueSkillRating.Mean, CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y, VanillaTrueskillSystemManager.RatingConversion.To_MyRating).ToString("F4");
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
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = VanillaTrueskillSystemManager.instance.ConvertRating((float)p.playerData.TrueSkillRating.Mean, CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y, VanillaTrueskillSystemManager.RatingConversion.To_MyRating).ToString("F4");
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
                        allShowBoxes[i].eloTxtGO.GetComponent<TMPro.TMP_Text>().text = VanillaTrueskillSystemManager.instance.ConvertRating((float)p.playerData.TrueSkillRating.Mean, CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y, VanillaTrueskillSystemManager.RatingConversion.To_MyRating).ToString("F4");
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

    public void ShowAddPlayerScreen()
    {
        _addPlayerScreenGO.SetActive(true);
    }

    public void CancelAddPlayer()
    {
        _addPlayerScreenGO.SetActive(false);
        _newPlayerSmurfCheckboxGO.GetComponent<Toggle>().isOn = false;
        _newPlayerRatingGO.GetComponent<TMP_InputField>().text = "";
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
