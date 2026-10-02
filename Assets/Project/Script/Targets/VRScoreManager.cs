using UnityEngine;

public static class VRScoreManager
{
    private const string HighScoreKey = "VRShooter.HighScore";

    public static int Score { get; private set; }
    public static float TimeRemaining { get; private set; }
    public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

    public static void AddScore(int points)
    {
        Score += Mathf.Max(points, 0);
        if (Score > HighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, Score);
            PlayerPrefs.Save();
        }
    }

    public static void ResetScore()
    {
        Score = 0;
    }

    public static void ResetRound(float duration)
    {
        Score = 0;
        TimeRemaining = Mathf.Max(duration, 0f);
    }

    public static void Tick(float deltaTime)
    {
        TimeRemaining = Mathf.Max(TimeRemaining - deltaTime, 0f);
    }
}
