using SQLite;

[Table("players")]
public class PlayerData
{
    [PrimaryKey, Unique, NotNull]
    public string username { get; set; }
    public int stage { get; set; }

    public PlayerData() { }

    public PlayerData(string username)
    {
        this.username = username;
        stage = 1;
    }
}
