namespace server.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nickname { get; set; } = "";
    public int Score { get; set; }
}
