using DilmerGames.Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private GameObject panelUI;

    [SerializeField]
    private Button startServerButton;

    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private TextMeshProUGUI playersInGameText;    

    private bool hasServerStarted;

    private void Awake()
    {
        Cursor.visible = true;
    }

    private void SetUserName()
    {

    }
    void Update()
    {        
        playersInGameText.text = $"Players in game: {PlayersManager.Instance.PlayersInGame}";
        
        if(hasServerStarted)
        {
            panelUI.SetActive(false);
            Cursor.visible = false;
        }
        SetUserName();
    }

    void Start()
    {
        // START SERVER
        startServerButton?.onClick.AddListener(() =>
        {
            panelUI.SetActive(false);
            if (NetworkManager.Singleton.StartServer())
            {   
                Logger.Instance.LogInfo("Server started...");
                
            }
                
            else
                Logger.Instance.LogInfo("Unable to start server...");
        });

        // START HOST
        startHostButton?.onClick.AddListener(() =>
        {
            panelUI.SetActive(false);
            if (NetworkManager.Singleton.StartHost())
            {
                
                Logger.Instance.LogInfo("Host started...");
            }               
            else
                Logger.Instance.LogInfo("Unable to start host...");
        });

        // START CLIENT
        startClientButton?.onClick.AddListener(() =>
        {
            panelUI.SetActive(false);
            if(NetworkManager.Singleton.StartClient())
            {
                Logger.Instance.LogInfo("Client started...");
            }               
            else
                Logger.Instance.LogInfo("Unable to start client...");
        });

        // STATUS TYPE CALLBACKS
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Logger.Instance.LogInfo($"{id} just connected...");
        };

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            hasServerStarted = true;
        };
        
    }
}
