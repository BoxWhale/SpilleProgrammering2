using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    SQLiteConnection _db;
    readonly Dictionary<string, PlayerData> _cache = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var dbPath = Path.Combine(Application.persistentDataPath, "gameData.db");
        _db = new SQLiteConnection(dbPath);
        _db.CreateTable<PlayerData>();
    }

    public PlayerData GetPlayerData(string username)
    {
        if (_cache.TryGetValue(username, out var pd))
            return pd;

        pd = _db.Find<PlayerData>(username);
        if (pd == null)
        {
            pd = new PlayerData(username);
            _db.Insert(pd);
        }
        return pd;
    }

    public bool PlayerDataExists(string username)
    {
        if (_cache.ContainsKey(username))
            return true;

        var pd = _db.Find<PlayerData>(username);
        if (pd != null)
        {
            _cache[username] = pd;
            return true;
        }
        return false;
    }

    public void SavePlayerData(PlayerData data, bool evictFromCache = false)
    {
        _db.InsertOrReplace(data);
        if (evictFromCache)
            _cache.Remove(data.username);
    }

    public void SaveAllAndClearCache()
    {
        foreach (var pd in _cache.Values)
            _db.InsertOrReplace(pd);
        _cache.Clear();
    }

    void OnDestroy()
    {
        SaveAllAndClearCache();
        _db?.Close();
    }
}
