namespace DemonAttackClone.Managers;

public enum GameState
{
    Title,
    Playing,
    GameOver
}

public class GameStateManager
{
    public GameState CurrentState { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int HighScore { get; private set; }
    public bool WaveClearedWithoutDamage { get; private set; }
    public bool IsNewHighScore { get; private set; }

    private const int StartingLives = 3;
    private const int WaveClearBonus = 100;

    private readonly string _saveFilePath;

    public GameStateManager()
    {
        CurrentState = GameState.Title;

        // Save file in user's local app data
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string gameFolder = Path.Combine(appDataPath, "DemonAttackClone");
        Directory.CreateDirectory(gameFolder);
        _saveFilePath = Path.Combine(gameFolder, "highscore.dat");

        LoadHighScore();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Score = 0;
        Lives = StartingLives;
        WaveClearedWithoutDamage = true;
        IsNewHighScore = false;
    }

    public void AddScore(int points)
    {
        Score += points;
        if (Score > HighScore)
        {
            HighScore = Score;
            IsNewHighScore = true;
        }
    }

    public void AwardWaveClearBonus(int waveNumber)
    {
        if (WaveClearedWithoutDamage)
        {
            AddScore(WaveClearBonus * waveNumber);
        }
        WaveClearedWithoutDamage = true;
    }

    public bool LoseLife()
    {
        Lives--;
        WaveClearedWithoutDamage = false;

        if (Lives <= 0)
        {
            CurrentState = GameState.GameOver;
            if (IsNewHighScore)
            {
                SaveHighScore();
            }
            return true;
        }

        return false;
    }

    public void ReturnToTitle()
    {
        CurrentState = GameState.Title;
    }

    private void LoadHighScore()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                string content = File.ReadAllText(_saveFilePath);
                if (int.TryParse(content.Trim(), out int savedScore))
                {
                    HighScore = savedScore;
                }
            }
        }
        catch
        {
            // If loading fails, just start with 0
            HighScore = 0;
        }
    }

    private void SaveHighScore()
    {
        try
        {
            File.WriteAllText(_saveFilePath, HighScore.ToString());
        }
        catch
        {
            // Silently fail if we can't save
        }
    }
}
