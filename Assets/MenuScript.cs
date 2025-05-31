using System;
using System.Net;
using TMPro;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    private DataManager _dataManager;
    private PlayerData _pd;
    [SerializeField] private NetworkManager netManager;
    public int defaultPort = 7777;

    [Header("Windows")]
    public CanvasGroup MainWindow;
    public CanvasGroup PlayWindow;

    [Header("Input Fields for Main Menu")]
    public TMP_InputField usernameInput;

    [Header("Input Fields for Play Menu")]
    public TMP_InputField addressConnect;
    public TMP_InputField portConnect;
    public TMP_InputField portHost;

    private void Awake()
    {
        _dataManager = GetComponent<DataManager>();
        DontDestroyOnLoad(gameObject);
        InitializeUI();

        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("NetworkManager not found! Add a NetworkManager to your scene.");
                return;
            }
        }

        netManager.offlineScene = SceneManager.GetActiveScene().name;
        Debug.Log($"NetworkManager configured Offline: {netManager.offlineScene}");

        CustomNetworkManager.OnClientConnectedEvent += OnClientConnected;
        CustomNetworkManager.OnClientDisconnectedEvent += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        CustomNetworkManager.OnClientConnectedEvent -= OnClientConnected;
        CustomNetworkManager.OnClientDisconnectedEvent -= OnClientDisconnected;
    }

    private void InitializeUI()
    {
        portConnect.text = defaultPort.ToString();
        addressConnect.text = "127.0.0.1";
        portHost.text = defaultPort.ToString();

        MainWindow.alpha = 1;
        MainWindow.blocksRaycasts = true;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;
    }

    #region UI Navigation

    public void OnStart()
    {
        string username = usernameInput.text;
        if (!string.IsNullOrEmpty(username))
        {
            _pd = _dataManager.GetPlayerData(username);
            PlayerPrefs.SetString("PlayerName", _pd.username);
            Debug.Log($"Player {_pd.username} loaded with Scene={_pd.SceneNumber}, Level={_pd.LevelNumber}");
        }
        else
        {
            Debug.LogWarning("No username provided. Using default values.");
            _pd = new PlayerData("DefaultPlayer");
        }
        OnWindowSwap();
    }

    public void OnExit()
    {
        Application.Quit();
    }

    public void OnReturn()
    {
        OnWindowSwap();
    }

    private void OnWindowSwap()
    {
        if (MainWindow.alpha == 1)
        {
            MainWindow.alpha = 0;
            MainWindow.blocksRaycasts = false;
            PlayWindow.alpha = 1;
            PlayWindow.blocksRaycasts = true;
        }
        else
        {
            MainWindow.alpha = 1;
            MainWindow.blocksRaycasts = true;
            PlayWindow.alpha = 0;
            PlayWindow.blocksRaycasts = false;
        }
    }

    private void HideAllWindows()
    {
        Debug.Log("Hiding all windows");
        MainWindow.alpha = 0;
        MainWindow.blocksRaycasts = false;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;
    }

    #endregion

    #region Networking

    public void OnHost()
    {
        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("Cannot start host: NetworkManager not found!");
                return;
            }
        }

        if (ushort.TryParse(portHost.text, out ushort port))
        {
            var kcpTransport = netManager.GetComponent<kcp2k.KcpTransport>();
            if (kcpTransport != null)
            {
                kcpTransport.Port = port;
                Debug.Log($"Host port set to {port}");
            }
        }

        // Make sure we have player data loaded
        if (_pd == null && PlayerPrefs.HasKey("PlayerName"))
        {
            string username = PlayerPrefs.GetString("PlayerName");
            _pd = _dataManager.GetPlayerData(username);
            Debug.Log($"Loaded player data for {username} before hosting");
        }

        // Set the online scene based on player's saved progress
        if (_pd != null)
        {
            netManager.onlineScene = _pd.SceneNumber.ToString();
            Debug.Log($"Setting online scene to {_pd.SceneNumber} based on saved player data");
        }
        else
        {
            netManager.onlineScene = "1"; // Default to scene 1
            Debug.Log("No player data found. Using default scene 1");
        }

        Debug.Log("Starting host with scene: " + netManager.onlineScene);
        netManager.StartHost();
    }

    public void OnConnect()
    {
        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("Cannot connect: NetworkManager not found!");
                return;
            }
        }
        var kcpTransport = netManager.GetComponent<kcp2k.KcpTransport>();
        if (kcpTransport == null)
        {
            Debug.LogError("KCP Transport not found on NetworkManager!");
            return;
        }

        if (ushort.TryParse(portConnect.text, out ushort port))
        {
            kcpTransport.Port = port;
            Debug.Log($"Connect port set to {port}");
        }
        else
        {
            Debug.Log($"Invalid port number {portConnect.text}. Using default port {defaultPort}.");
            port = (ushort)defaultPort;
            kcpTransport.Port = port;
        }

        string address = addressConnect.text.Trim();
        address = address.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
        if (!IPAddress.TryParse(address, out _) && !address.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            if (address.Contains(" ") || address.Contains(":") || !address.Contains("."))
            {
                Debug.LogWarning("Address doesn't appear to be a valid IP address or hostname");
            }
        }
        netManager.networkAddress = address;
        
        // When connecting as a client, DO NOT set onlineScene
        // The host will control scene loading and synchronization
        Debug.Log($"Connecting to {address}:{port} - Scene will be determined by host");
        netManager.StartClient();
    }

    private void OnClientConnected()
    {
        Debug.Log("Successfully connected to server");
        HideAllWindows();
    }

    private void OnClientDisconnected()
    {
        Debug.Log("Disconnected from server");
        
        if (_dataManager != null)
        {
            _dataManager.SaveAllAndClearCache();
            Debug.Log("Player data saved to disk on disconnect");
        }

        if (NetworkServer.active)
        {
            NetworkServer.Shutdown();
        }

        if (NetworkClient.isConnected)
        {
            NetworkClient.Disconnect();
        }

        if (SceneManager.GetActiveScene().name != netManager.offlineScene)
        {
            SceneManager.LoadScene(netManager.offlineScene);
        }
        
        MainWindow.alpha = 1;
        MainWindow.blocksRaycasts = true;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;
    }
    #endregion
}
