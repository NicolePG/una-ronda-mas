using UnityEngine;

/// <summary>
/// Guardado con PlayerPrefs (en WebGL queda en el navegador).
/// Récord de ronda, récord de estrellas, partidas jugadas y volúmenes.
/// </summary>
public static class SaveManager
{
    public const float DefaultVolume = 0.8f;

    private const string KeyBestRound = "URM_BestRound";
    private const string KeyBestStars = "URM_BestStars";
    private const string KeyGamesPlayed = "URM_GamesPlayed";
    private const string KeyMusicVolume = "URM_MusicVolume";
    private const string KeySfxVolume = "URM_SfxVolume";

    public static int BestRound => PlayerPrefs.GetInt(KeyBestRound, 0);
    public static int BestStars => PlayerPrefs.GetInt(KeyBestStars, 0);

    /// <summary>Partidas terminadas (sube en cada Eliminado). Desde 1, el menú cambia.</summary>
    public static int GamesPlayed => PlayerPrefs.GetInt(KeyGamesPlayed, 0);

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KeyMusicVolume, DefaultVolume);
        set => SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(KeySfxVolume, DefaultVolume);
        set => SetFloat(KeySfxVolume, Mathf.Clamp01(value));
    }

    /// <summary>Guarda el resultado de una partida y devuelve los datos para la pantalla Eliminado.</summary>
    public static GameStats RecordGame(int roundReached, int totalStars)
    {
        int previousRound = BestRound;
        int previousStars = BestStars;
        bool newBestRound = roundReached > previousRound;
        bool newBestStars = totalStars > previousStars;

        if (newBestRound) PlayerPrefs.SetInt(KeyBestRound, roundReached);
        if (newBestStars) PlayerPrefs.SetInt(KeyBestStars, totalStars);
        int games = GamesPlayed + 1;
        PlayerPrefs.SetInt(KeyGamesPlayed, games);
        PlayerPrefs.Save();

        return new GameStats(roundReached, totalStars,
            Mathf.Max(previousRound, roundReached), Mathf.Max(previousStars, totalStars),
            games, newBestRound, newBestStars);
    }

    /// <summary>Borra récords, partidas y volúmenes.</summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KeyBestRound);
        PlayerPrefs.DeleteKey(KeyBestStars);
        PlayerPrefs.DeleteKey(KeyGamesPlayed);
        PlayerPrefs.DeleteKey(KeyMusicVolume);
        PlayerPrefs.DeleteKey(KeySfxVolume);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Para probar el menú que cambia según las partidas jugadas.</summary>
    public static void DebugSetGamesPlayed(int games)
    {
        PlayerPrefs.SetInt(KeyGamesPlayed, Mathf.Max(0, games));
        PlayerPrefs.Save();
    }
#endif

    private static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }
}
