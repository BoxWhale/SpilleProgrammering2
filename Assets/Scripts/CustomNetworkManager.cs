using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class CustomNetworkManager : NetworkManager
{
    public static new CustomNetworkManager singleton => (CustomNetworkManager)NetworkManager.singleton;

    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject playerPrefabToSpawn;

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        
        RegisterSpawnablePrefabs();
    }

    private void RegisterSpawnablePrefabs()
    {
        if (playerPrefabToSpawn != null && playerPrefabToSpawn.GetComponent<NetworkIdentity>() != null)
        {
            NetworkClient.RegisterPrefab(playerPrefabToSpawn);
        }
    }

    /// <summary>
    /// Called on the server when a client connects
    /// </summary>
    /// <param name="conn">The connection that connected</param>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Client connected: {conn.connectionId}");
        base.OnServerConnect(conn);
    }

    /// <summary>
    /// Called on the server when a client disconnects
    /// </summary>
    /// <param name="conn">The connection that disconnected</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Client disconnected: {conn.connectionId}");
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called on the server when a client is ready
    /// </summary>
    /// <param name="conn">The connection that is ready</param>
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        Debug.Log($"Client ready: {conn.connectionId}");
    }

    /// <summary>
    /// Called on the server when a client adds a new player
    /// </summary>
    /// <param name="conn">The connection that added the player</param>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (playerPrefabToSpawn == null)
        {
            base.OnServerAddPlayer(conn);
            return;
        }

        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefabToSpawn, startPos.position, startPos.rotation)
            : Instantiate(playerPrefabToSpawn);

        player.name = $"{playerPrefabToSpawn.name} [connId={conn.connectionId}]";
        
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    /// <summary>
    /// Called when the server is started
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started!");
    }

    /// <summary>
    /// Called when the client is started
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started!");
    }

    /// <summary>
    /// Server method to spawn a prefab at a specific position and rotation
    /// </summary>
    /// <param name="prefabToSpawn">The prefab to spawn</param>
    /// <param name="position">Position to spawn at</param>
    /// <param name="rotation">Rotation to spawn with</param>
    /// <returns>The spawned GameObject</returns>
    public GameObject SpawnPrefab(GameObject prefabToSpawn, Vector3 position, Quaternion rotation)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Cannot spawn objects when the server is not active!");
            return null;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("Cannot spawn a null prefab!");
            return null;
        }

        if (prefabToSpawn.GetComponent<NetworkIdentity>() == null)
        {
            Debug.LogError($"Prefab {prefabToSpawn.name} does not have a NetworkIdentity component!");
            return null;
        }

        GameObject spawnedObject = Instantiate(prefabToSpawn, position, rotation);
        NetworkServer.Spawn(spawnedObject);
        
        Debug.Log($"Spawned {prefabToSpawn.name} at {position}");
        return spawnedObject;
    }
}