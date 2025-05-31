using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public static event Action OnClientConnectedEvent;
    public static event Action OnClientDisconnectedEvent;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (DataManager.Instance == null)
            new GameObject("DatabaseManager")
                .AddComponent<DataManager>();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Debug.LogWarning("Player spawn attempted in an invalid scene.");
            return;
        }
        base.OnServerAddPlayer(conn);

        string username = conn.connectionId.ToString();
        var data = DataManager.Instance.GetPlayerData(username);

        var stats = conn.identity.GetComponent<PlayerStats>();
        stats.stage = data.stage;
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var stats = conn.identity.GetComponent<PlayerStats>();
            string username = conn.connectionId.ToString();
            var data = DataManager.Instance.GetPlayerData(username);
            data.stage = stats.stage;
            DataManager.Instance.SavePlayerData(data, evictFromCache: true);
        }
        base.OnServerDisconnect(conn);
    }
    
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnectedEvent?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnectedEvent?.Invoke();
    }
}
