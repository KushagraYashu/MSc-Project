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
    [SerializeField] private GameObject _sysNameTxtGO;

    [SerializeField] private GameObject _systemDropDownGO;
    [SerializeField] private GameObject _matchesPerPlayerInputFieldGO;

    [SerializeField] private GameObject[] _poolGOs;
    [SerializeField] private GameObject[] _poolPlayerCountTxtGOs;

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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
