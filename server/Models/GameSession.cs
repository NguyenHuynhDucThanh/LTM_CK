namespace server.Models;

public class GameSession
{
    public string State { get; set; } = "WAITING";
    public int CurrentIndex { get; set; } = -1;
    public Question? CurrentQuestion { get; set; }
    public DateTime QuestionStartedAt { get; set; }
    public int TimeLimitSeconds { get; set; } = 20;
    public Dictionary<string, string> Answers { get; set; } = [];
}
