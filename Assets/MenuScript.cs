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
    public TMP_InputField usernameInput;

    [Header("Input Fields for Play Menu")]
    public TMP_InputField addressConnect;
    public TMP_InputField portConnect;
    public TMP_InputField portHost;

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
        //load online scene attached to PlayerData
        
        netManager.onlineScene = onlineSceneName;
        
        
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
        
            // Get the KCP transport from NetworkManager
            var kcpTransport = netManager.GetComponent<kcp2k.KcpTransport>();
            if (kcpTransport == null)
            {
                Debug.LogError("KCP Transport not found on NetworkManager!");
                return;
            }
        
            // Validate and set port
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
        
            // Allow both IP addresses and hostnames
            string address = addressConnect.text.Trim();
            // Remove invisible Unicode characters (like zero-width space)
            address = address.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
            // Validate IP address format
            if (!IPAddress.TryParse(address, out _) && !address.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Not a valid IP address format, but could be a hostname
                if (address.Contains(" ") || address.Contains(":") || !address.Contains("."))
                {
                    Debug.LogWarning("Address doesn't appear to be a valid IP address or hostname");
                    // Continue anyway as it might be a hostname
                }
            }
        
            // Configure network settings
            netManager.networkAddress = address;
        
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
            
            HideAllWindows();
        }

    private void OnClientConnected()
    {
        Debug.Log("Successfully connected to server");
        HideAllWindows();
    }

    private void OnClientDisconnected()
    {
        Debug.Log("Disconnected from server");

        // If we're the host that stopped, ensure all clients are disconnected
        if (NetworkServer.active)
        {
            NetworkServer.Shutdown();
        }

        // Make sure client is fully stopped if still connected
        if (NetworkClient.isConnected)
        {
            NetworkClient.Disconnect();
        }

        // Return to offline scene if we're not already there
        if (SceneManager.GetActiveScene().name != netManager.offlineScene)
        {
            SceneManager.LoadScene(netManager.offlineScene);
        }

        // Show main menu again
        MainWindow.alpha = 1;
        MainWindow.blocksRaycasts = true;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;
    }
    #endregion
}