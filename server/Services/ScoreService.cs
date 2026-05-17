namespace server.Services;

public class ScoreService
{
    private const int BaseScore = 100;
    private const int TimeLimitSeconds = 20;
    private const int BonusMultiplier = 5;

    public int Calculate(bool isCorrect, double elapsedSeconds)
    {
        if (!isCorrect) return 0;

        var bonus = Math.Max(0, (int)((TimeLimitSeconds - elapsedSeconds) * BonusMultiplier));
        return BaseScore + bonus;
    }
}
