using System;
using System.Net;
using TMPro;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    // Assign this in the Inspector
    [SerializeField] private NetworkManager netManager;
    public int defaultPort = 7777;

    [Header("Windows")]
    public CanvasGroup MainWindow;
    public CanvasGroup PlayWindow;

    [Header("Input Fields for Main Menu")]
    public TMP_Text usernameInput;

    [Header("Input Fields for Play Menu")]
    public TMP_Text addressConnect;
    public TMP_Text portConnect;
    public TMP_Text portHost;

    [Header("Scene Settings")]
    public string onlineSceneName = "SampleScene";

    private void Awake()
    {
        // Keep menu across scene changes
        DontDestroyOnLoad(gameObject);
        
        // Initialize UI (doesn't depend on NetworkManager)
        InitializeUI();
        
        // Find NetworkManager if not assigned in inspector
        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            
            if (netManager == null)
            {
                Debug.LogError("NetworkManager not found! Add a NetworkManager to your scene.");
                return;
            }
        }
        
        // Configure scenes
        netManager.onlineScene = onlineSceneName;
        netManager.offlineScene = SceneManager.GetActiveScene().name;
        Debug.Log($"NetworkManager configured - Online: {onlineSceneName}, Offline: {netManager.offlineScene}");
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        NetworkClient.OnConnectedEvent -= OnClientConnected;
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
    }

    private void InitializeUI()
    {
        // Set default values
        portConnect.text = defaultPort.ToString();
        addressConnect.text = "127.0.0.1";
        portHost.text = defaultPort.ToString();

        // Initialize windows
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
            PlayerData data = new PlayerData(username);
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
        MainWindow.alpha = 0;
        MainWindow.blocksRaycasts = false;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;
    }

    #endregion

    #region Networking

    public void OnHost()
    {
        // Check if NetworkManager is available
        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("Cannot start host: NetworkManager not found!");
                return;
            }
        }

        // Configure port
        if (ushort.TryParse(portHost.text, out ushort port))
        {
            var transport = Transport.active as TelepathyTransport;
            if (transport != null)
            {
                transport.port = port;
                Debug.Log($"Host port set to {port}");
            }
        }

        // Ensure scene is set
        netManager.onlineScene = onlineSceneName;

        // Unregister previous callbacks
        NetworkClient.OnConnectedEvent -= OnClientConnected;
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;

        // Register new callbacks
        NetworkClient.OnConnectedEvent += OnClientConnected;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;

        // Start host
        Debug.Log("Starting host with scene: " + netManager.onlineScene);
        netManager.StartHost();

        // Hide menu windows
        HideAllWindows();
    }

    public void OnConnect()
    {
        // Check if NetworkManager is available
        if (netManager == null)
        {
            netManager = FindObjectOfType<NetworkManager>();
            if (netManager == null)
            {
                Debug.LogError("Cannot connect: NetworkManager not found!");
                return;
            }
        }

        // Validate connection parameters
        if (!ushort.TryParse(portConnect.text, out ushort port) ||
            !IPAddress.TryParse(addressConnect.text, out IPAddress address))
        {
            Debug.LogError("Invalid connection parameters");
            return;
        }

        // Configure network settings
        netManager.networkAddress = address.ToString();

        var transport = Transport.active as TelepathyTransport;
        if (transport != null)
        {
            transport.port = port;
        }

        // Ensure scene is set
        netManager.onlineScene = onlineSceneName;

        // Unregister previous callbacks
        NetworkClient.OnConnectedEvent -= OnClientConnected;
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;

        // Register new callbacks
        NetworkClient.OnConnectedEvent += OnClientConnected;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;

        // Start client
        Debug.Log($"Connecting to {address}:{port}");
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

        // Show main menu again
        MainWindow.alpha = 1;
        MainWindow.blocksRaycasts = true;
    }
    #endregion
}