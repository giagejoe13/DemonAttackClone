using System.Text.Json;

namespace DemonAttackClone.Managers;

public record HighScoreEntry(int Score, string Initials, DateTime Date);

public class HighScoreManager
{
    private const int MaxScores = 10;
    private const string FileName = "highscores.json";

    private List<HighScoreEntry> _scores = new();
    private readonly string _filePath;

    public IReadOnlyList<HighScoreEntry> Scores => _scores.AsReadOnly();

    public HighScoreManager()
    {
        // Store high scores in user's local app data
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string gameFolder = Path.Combine(appDataPath, "DemonAttackClone");
        Directory.CreateDirectory(gameFolder);
        _filePath = Path.Combine(gameFolder, FileName);
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<HighScoreEntry>>(json);
                if (loaded != null)
                {
                    _scores = loaded.OrderByDescending(s => s.Score).Take(MaxScores).ToList();
                }
            }
        }
        catch
        {
            // If loading fails, start with empty scores
            _scores = new List<HighScoreEntry>();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_scores, options);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silently fail if we can't save
        }
    }

    public bool IsHighScore(int score)
    {
        if (score <= 0) return false;
        if (_scores.Count < MaxScores) return true;
        return score > _scores[^1].Score;
    }

    public void AddScore(int score, string initials)
    {
        var entry = new HighScoreEntry(score, initials.ToUpperInvariant(), DateTime.Now);
        _scores.Add(entry);
        _scores = _scores.OrderByDescending(s => s.Score).Take(MaxScores).ToList();
        Save();
    }

    public int GetHighestScore()
    {
        return _scores.Count > 0 ? _scores[0].Score : 0;
    }
}
