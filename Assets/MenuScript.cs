using System;
using System.Collections;
using System.Net;
using TMPro;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    // Define custom message types
    private struct SceneRequestMessage : NetworkMessage { }
    private struct SceneResponseMessage : NetworkMessage
    {
        public string sceneName;
    }
    public struct HostMessage : NetworkMessage { }
    public struct ConnectMessage : NetworkMessage { }
    public struct DisconnectMessage : NetworkMessage { }
    private NetworkManager netManager;
    private PlayerData playerData;
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
    public int levelID = 1;
    public string hostSceneName = "SampleScene";
    
     private void Start()
    {
        portConnect.text = defaultPort.ToString();
        addressConnect.text = "127.0.0.1";
        portHost.text = defaultPort.ToString();
        netManager = NetworkManager.singleton;
        MainWindow.alpha = 1;
        MainWindow.blocksRaycasts = true;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false;

    }
    


     public void OnStart()
    {
        //playerData = new PlayerData(usernameInput.GetComponent<TMP_InputField>().text);
        Debug.Log(usernameInput.GetComponent<TMP_Text>().text);
        //hostSceneName = SceneManager.GetSceneByBuildIndex(StageID).name;
        NetworkClient.RegisterHandler<SceneResponseMessage>(msg =>
        {
            Debug.Log($"Received scene name from host: {msg.sceneName}");
            SceneLoader.LoadLevel(msg.sceneName);
            SceneLoader.ShowLoadingScreen();
        });
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
        if (MainWindow.alpha == 1 && PlayWindow.alpha == 0)
        {
            MainWindow.alpha = 0;
            MainWindow.blocksRaycasts = false;
            PlayWindow.alpha = 1;
            PlayWindow.blocksRaycasts = true;
        }
        else if (MainWindow.alpha == 0 && PlayWindow.alpha == 1)
        {
            MainWindow.alpha = 1;
            MainWindow.blocksRaycasts = true;
            PlayWindow.alpha = 0;
            PlayWindow.blocksRaycasts = false;
        }
        else
        {
            MainWindow.alpha = 1;
            MainWindow.blocksRaycasts = true;
            PlayWindow.alpha = 0;
            PlayWindow.blocksRaycasts = false;
        }
    }

     private void OnSceneChange()
    {
        MainWindow.alpha = 0;
        MainWindow.blocksRaycasts = false;
        PlayWindow.alpha = 0;
        PlayWindow.blocksRaycasts = false; 
    }

     public void OnHost()
    {
        ushort port;
        // Check if the port is valid
        if (ushort.TryParse(portHost.text, out port))
        {
            var transport = Transport.active as TelepathyTransport;
            if (transport != null)
            {
                transport.port = port;
                Debug.Log($"Host port set to {port}");
            }
            else
            {
                Debug.LogError("Current port is not valid");
                return;
            }
            netManager.transport = transport;
        }

        // Check if the scene is not the main menu
        if (!SceneManager.GetSceneByName(hostSceneName).isLoaded)
        {
            SceneManager.LoadScene(hostSceneName, LoadSceneMode.Additive);
            StartCoroutine(WaitForSceneAndHost(hostSceneName));
        }
        else
        {
            StartCoroutine(WaitForSceneAndHost(hostSceneName));
        }
    }
     public void OnConnect()
    {
        ushort port;
        IPAddress address;
        var transport = Transport.active as TelepathyTransport;

        if (ushort.TryParse(portConnect.text, out port))
        {
            if (transport != null)
            {
                transport.port = port;
            }
        }
        else
        {
            Debug.LogError("Current port is not valid");
            return;
        }

        if (IPAddress.TryParse(addressConnect.text, out address))
        {
            if (IsHostReachable(address, port))
            {
                Debug.Log($"Host is reachable at {address}:{port}");
                netManager.networkAddress = address.ToString();
                netManager.StartClient();
                            
                // Add connection status callbacks
                NetworkClient.RegisterHandler<ConnectMessage>(msg =>
                {
                    Debug.Log("Successfully connected to the host.");
                    RequestSceneFromHost();
                    NetworkClient.RegisterHandler<SceneResponseMessage>(response =>
                    {
                        StartCoroutine(CheckSceneLoaded(response.sceneName));
                    });
                    OnSceneChange();
                });
            }
            else
            {
                Debug.LogError($"Host is not reachable at {address}:{port}");
                NetworkClient.RegisterHandler<DisconnectMessage>(msg =>
                {
                    Debug.LogError("Failed to connect to the host.");
                });
                return;
            }
        }
        else
        {
            Debug.LogError("Current address is not valid");
            NetworkClient.RegisterHandler<DisconnectMessage>(msg =>
            {
                Debug.LogError("Failed to connect to the host.");
            });
            return;
        }
    }

    private IEnumerator WaitForSceneAndHost(string sceneName)
    {
        yield return StartCoroutine(CheckSceneLoaded(sceneName));

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            netManager.StartHost();
            NetworkClient.RegisterHandler<HostMessage>(msg =>
            {
                Debug.Log("Host started successfully.");
            });
            Debug.Log("Hosting started in the loaded scene.");
            OnSceneChange();
        }
        else
        {
            Debug.LogError("No valid scene is loaded for hosting.");
        }
    }
    private static IEnumerator CheckSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        while (!scene.isLoaded)
        {
            yield return null; // Wait for the next frame
            scene = SceneManager.GetSceneByName(sceneName);
        }
        Debug.Log($"Scene '{sceneName}' is successfully loaded.");
        SceneManager.SetActiveScene(scene);
    }
    private static bool IsHostReachable(IPAddress ipAddress, int port)
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var result = client.BeginConnect(ipAddress, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                if (!success)
                    return false;

                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

     private static void RequestSceneFromHost()
    {
        NetworkClient.Send(new SceneRequestMessage());

        NetworkClient.RegisterHandler<SceneResponseMessage>(msg =>
        {
            Debug.Log($"Received scene name from host: {msg.sceneName}");
            if (!SceneManager.GetSceneByName(msg.sceneName).isLoaded)
            {
                SceneManager.LoadScene(msg.sceneName, LoadSceneMode.Additive);
            }
        });
    }
}
