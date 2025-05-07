using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (DataManager.Instance == null)
            new GameObject("DatabaseManager")
                .AddComponent<DataManager>();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
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
}
