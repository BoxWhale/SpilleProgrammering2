using SQLite;

[Table("players")]
public class PlayerData
{
    [PrimaryKey]
    public string username { get; set; }
    public int stage { get; set; }

    public PlayerData() { }

    public PlayerData(string id)
    {
        username = id;
        stage = 1;
    }
}
