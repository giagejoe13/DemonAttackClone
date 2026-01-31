namespace DemonAttackClone.Managers;

public enum GameState
{
    Title,
    Playing,
    GameOver,
    EnteringInitials
}

public class GameStateManager
{
    public GameState CurrentState { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int HighScore { get; private set; }
    public bool WaveClearedWithoutDamage { get; private set; }
    public bool IsNewHighScore { get; private set; }

    // High score initials entry
    public HighScoreManager HighScoreManager { get; }
    public char[] PlayerInitials { get; } = { 'A', 'A', 'A' };
    public int InitialsCursorPosition { get; set; }

    private const int StartingLives = 3;
    private const int MaxLives = 3;
    private const int WaveClearBonus = 100;

    public GameStateManager()
    {
        CurrentState = GameState.Title;
        HighScoreManager = new HighScoreManager();
        HighScore = HighScoreManager.GetHighestScore();
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

        // Award an extra life for clearing the wave (max 3 lives)
        if (Lives < MaxLives)
        {
            Lives++;
        }
    }

    public bool LoseLife()
    {
        Lives--;
        WaveClearedWithoutDamage = false;

        if (Lives <= 0)
        {
            CurrentState = GameState.GameOver;
            return true;
        }

        return false;
    }

    public void ReturnToTitle()
    {
        CurrentState = GameState.Title;
    }

    public void CheckAndEnterHighScore()
    {
        if (HighScoreManager.IsHighScore(Score))
        {
            ResetInitials();
            CurrentState = GameState.EnteringInitials;
        }
        else
        {
            CurrentState = GameState.Title;
        }
    }

    public void ResetInitials()
    {
        PlayerInitials[0] = 'A';
        PlayerInitials[1] = 'A';
        PlayerInitials[2] = 'A';
        InitialsCursorPosition = 0;
    }

    public void SetCurrentInitial(char letter)
    {
        if (letter >= 'A' && letter <= 'Z')
        {
            PlayerInitials[InitialsCursorPosition] = letter;
        }
    }

    public void CycleInitialUp()
    {
        char c = PlayerInitials[InitialsCursorPosition];
        c = c == 'Z' ? 'A' : (char)(c + 1);
        PlayerInitials[InitialsCursorPosition] = c;
    }

    public void CycleInitialDown()
    {
        char c = PlayerInitials[InitialsCursorPosition];
        c = c == 'A' ? 'Z' : (char)(c - 1);
        PlayerInitials[InitialsCursorPosition] = c;
    }

    public void MoveCursorLeft()
    {
        InitialsCursorPosition = Math.Max(0, InitialsCursorPosition - 1);
    }

    public void MoveCursorRight()
    {
        InitialsCursorPosition = Math.Min(2, InitialsCursorPosition + 1);
    }

    public void SubmitHighScore()
    {
        string initials = new string(PlayerInitials);
        HighScoreManager.AddScore(Score, initials);
        HighScore = HighScoreManager.GetHighestScore();
        CurrentState = GameState.Title;
    }
}
