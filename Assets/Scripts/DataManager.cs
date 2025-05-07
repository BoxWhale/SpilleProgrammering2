using System.IO;
using SQLite;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    SQLiteConnection _db;

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
        var pd = _db.Find<PlayerData>(username);
        if (pd == null)
        {
            pd = new PlayerData(username);
            _db.Insert(pd);
        }
        return pd;
    }

    public void SavePlayerData(PlayerData data)
    {
        _db.InsertOrReplace(data);
    }

    void OnDestroy()
    {
        _db?.Close();
    }
}
