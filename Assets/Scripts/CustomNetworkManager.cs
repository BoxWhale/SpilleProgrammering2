using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public static event Action OnClientConnectedEvent;
    public static event Action OnClientDisconnectedEvent;

    private bool serverStarted;

    // This method is called when the server starts
    public override void OnStartServer()
    {
        base.OnStartServer();

        serverStarted = true;
    }   
    // This method is called when the server is stopped
    public override void OnStopServer()
    {
        Debug.Log("Server stopping - Saving all player data");
        if (DataManager.Instance != null) DataManager.Instance.SaveAllAndClearCache();

        serverStarted = false;
        base.OnStopServer();
    }
    // This method is called when a player is added to or joins the server
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        /* if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Debug.LogWarning("Player spawn attempted in an invalid scene.");
            return;
        } */

        //base.OnServerAddPlayer(conn);

        // Load player data from the database
        var username = "DefaultPlayer";
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            var playerPrefsName = PlayerPrefs.GetString("PlayerName");
            if (!string.IsNullOrEmpty(playerPrefsName))
            {
                username = playerPrefsName;
            }
        }

        Debug.Log($"Loading player data for {username}");
        var playerData = DataManager.Instance.GetPlayerData(username);

        // Find the correct checkpoint in the correct scene
        LevelCheckPoint[] checkpoints = FindObjectsByType<LevelCheckPoint>(FindObjectsSortMode.None);
        LevelCheckPoint spawnCheckpoint = null;
        foreach (var cp in checkpoints)
        {
            var idField = typeof(LevelCheckPoint).GetField("checkpointId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int cpId = (int)idField.GetValue(cp);
            if (cpId == playerData.LevelNumber && cp.gameObject.scene.buildIndex == playerData.SceneNumber)
            {
                spawnCheckpoint = cp;
                break;
            }
        }

        Vector3 spawnPosition = spawnCheckpoint != null ? spawnCheckpoint.transform.position : Vector3.zero;
        Quaternion spawnRotation = spawnCheckpoint != null ? spawnCheckpoint.transform.rotation : Quaternion.identity;

        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        NetworkServer.AddPlayerForConnection(conn, playerObj);

        // Update player stats with loaded data
        var stats = conn.identity.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.displayData = playerData.stage;
            stats.scene = playerData.SceneNumber;
            stats.level = playerData.LevelNumber;

            Debug.Log($"Player {username} data loaded: Scene={stats.scene}, Level={stats.level}");

            // Check if we need to change the scene (only for the host player)
            if (serverStarted && NetworkServer.connections.Count == 1 &&
                SceneManager.GetActiveScene().buildIndex != stats.scene)
            {
                Debug.Log($"Host player connected - changing to scene {stats.scene}");
                ServerChangeScene(stats.scene.ToString());
            }
        }
    }

    // This method is called when a player disconnects from the server
    // and saves the player's data to the database
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var username = conn.connectionId.ToString();
            var player = conn.identity.GetComponent<PlayerNetworkScript>();
            if (player != null && !string.IsNullOrEmpty(player.playerName)) username = player.playerName;

            var stats = conn.identity.GetComponent<PlayerStats>();
            if (stats != null)
            {
                var data = DataManager.Instance.GetPlayerData(username);
                data.SceneNumber = stats.scene;
                data.LevelNumber = stats.level;

                Debug.Log($"Saving player data for {username}: Scene={stats.scene}, Level={stats.level}");
                DataManager.Instance.SavePlayerData(data, true);
            }
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