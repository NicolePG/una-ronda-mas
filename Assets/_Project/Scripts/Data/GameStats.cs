/// <summary>Resultado de una partida, para la pantalla Eliminado.</summary>
public class GameStats
{
    public int RoundReached { get; }
    public int TotalStars { get; }
    public int BestRound { get; }
    public int BestStars { get; }
    public int GamesPlayed { get; }
    public bool IsNewBestRound { get; }
    public bool IsNewBestStars { get; }

    public GameStats(int roundReached, int totalStars, int bestRound, int bestStars,
        int gamesPlayed, bool isNewBestRound, bool isNewBestStars)
    {
        RoundReached = roundReached;
        TotalStars = totalStars;
        BestRound = bestRound;
        BestStars = bestStars;
        GamesPlayed = gamesPlayed;
        IsNewBestRound = isNewBestRound;
        IsNewBestStars = isNewBestStars;
    }
}
