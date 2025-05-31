using SQLite;

[Table("players")]
public class PlayerData
{
    [PrimaryKey, Unique, NotNull]
    public string username { get; set; }
    public int stage { get; set; }

    #region BitPacking Stage_Variable

    [Ignore] // Scene number is stored in the upper 16 bits of stage
    public int SceneNumber
    {
        get => stage >> 16;
        set => stage = (value << 16) | (LevelNumber);
    }

    [Ignore] // Level number is stored in the lower 16 bits of stage
    public int LevelNumber
    {
        get => stage & 0xFFFF;
        set => stage = (SceneNumber << 16) | (value & 0xFFFF);
    }

    #endregion

    public PlayerData() { }

    public PlayerData(string username)
    {
        this.username = username;
        SceneNumber = 1;
        LevelNumber = 1;
    }
}
