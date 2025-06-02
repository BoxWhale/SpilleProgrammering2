using System;
using System.Net;
using kcp2k;
using Mirror;
using TMPro;
using UnityEngine;
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
        _dataManager = DataManager.Instance;
        DontDestroyOnLoad(gameObject);
        InitializeUI();
        InitializeNetworkManager();

        CustomNetworkManager.OnClientConnectedEvent += OnClientConnected;
        CustomNetworkManager.OnClientDisconnectedEvent += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        CustomNetworkManager.OnClientConnectedEvent -= OnClientConnected;
        CustomNetworkManager.OnClientDisconnectedEvent -= OnClientDisconnected;
    }

    private void InitializeNetworkManager()
    {
        if (netManager == null)
        {
            netManager = FindFirstObjectByType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("NetworkManager not found! Add a NetworkManager to your scene.");
                return;
            }
        }

        netManager.offlineScene = SceneManager.GetActiveScene().name;
        Debug.Log($"NetworkManager configured Offline: {netManager.offlineScene}");
    }

    private void InitializeUI()
    {
        portConnect.text = defaultPort.ToString();
        addressConnect.text = "127.0.0.1";
        portHost.text = defaultPort.ToString();

        ShowWindow(MainWindow);
        HideWindow(PlayWindow);
    }

    #region UI Navigation

    public void OnStart()
    {
        var username = !string.IsNullOrWhiteSpace(usernameInput.text) ? usernameInput.text.Trim() : "DefaultPlayer";
        PlayerPrefs.SetString("PlayerName", username);
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

    #region UI Canvas management

    private void OnWindowSwap()
    {
        if (MainWindow.alpha == 1)
        {
            SwitchWindows(MainWindow, PlayWindow);
        }
        else
        {
            SwitchWindows(PlayWindow, MainWindow);
        }
    }

    private void SwitchWindows(CanvasGroup fromWindow, CanvasGroup toWindow)
    {
        HideWindow(fromWindow);
        ShowWindow(toWindow);
    }

    private void ShowWindow(CanvasGroup window)
    {
        window.alpha = 1;
        window.blocksRaycasts = true;
    }

    private void HideWindow(CanvasGroup window)
    {
        window.alpha = 0;
        window.blocksRaycasts = false;
    }

    private void HideAllWindows()
    {
        Debug.Log("Hiding all windows");
        HideWindow(MainWindow);
        HideWindow(PlayWindow);
    }

    #endregion

    #endregion

    #region Networking

    public void OnHost()
    {
        if (ushort.TryParse(portHost.text, out var port))
        {
            var kcpTransport = netManager.GetComponent<KcpTransport>();
            if (kcpTransport != null)
            {
                kcpTransport.Port = port;
                Debug.Log($"Host port set to {port}");
            }
        }

        Debug.Log("Starting host with scene: " + netManager.onlineScene);
        netManager.StartHost();
    }

    public void OnConnect()
    {
        if (netManager == null)
        {
            netManager = FindFirstObjectByType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("Cannot connect: NetworkManager not found!");
                return;
            }
        }

        var kcpTransport = netManager.GetComponent<KcpTransport>();
        if (kcpTransport == null)
        {
            Debug.LogError("KCP Transport not found on NetworkManager!");
            return;
        }

        if (ushort.TryParse(portConnect.text, out var port))
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

        var address = addressConnect.text.Trim();
        address = address.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
        if (!IPAddress.TryParse(address, out _) && !address.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            if (address.Contains(" ") || address.Contains(":") || !address.Contains("."))
                Debug.LogWarning("Address doesn't appear to be a valid IP address or hostname");

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
        LoadOfflineScene();
        ShowMainWindow();
    }

    private void LoadOfflineScene()
    {
        if (SceneManager.GetActiveScene().name != netManager.offlineScene)
            SceneManager.LoadScene(netManager.offlineScene);
    }

    private void ShowMainWindow()
    {
        ShowWindow(MainWindow);
        HideWindow(PlayWindow);
    }

    #endregion
}

