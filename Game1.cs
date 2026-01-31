using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DemonAttackClone.Entities;
using DemonAttackClone.Managers;
using DemonAttackClone.Utils;

namespace DemonAttackClone;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // Game dimensions
    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;

    // Game objects
    private Player _player = null!;
    private Bullet _playerBullet = null!;
    private WaveManager _waveManager = null!;
    private GameStateManager _gameStateManager = null!;
    private SoundManager _soundManager = null!;

    // Input state
    private KeyboardState _previousKeyboardState;

    // Wave transition
    private float _waveTransitionTimer;
    private const float WaveTransitionDelay = 2f;
    private bool _waveCompleteTriggered;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.ApplyChanges();

        Window.Title = "Demon Attack Clone";

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Initialize shape renderer
        ShapeRenderer.Initialize(GraphicsDevice);

        // Create game objects
        _player = new Player(ScreenWidth, ScreenHeight);
        _playerBullet = new Bullet();
        _waveManager = new WaveManager(ScreenWidth, ScreenHeight);
        _gameStateManager = new GameStateManager();

        // Initialize sound
        _soundManager = new SoundManager();
        _soundManager.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        // Global exit
        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        switch (_gameStateManager.CurrentState)
        {
            case GameState.Title:
                UpdateTitle(keyboardState);
                break;
            case GameState.Playing:
                UpdatePlaying(gameTime, keyboardState);
                break;
            case GameState.GameOver:
                UpdateGameOver(keyboardState);
                break;
            case GameState.EnteringInitials:
                UpdateEnteringInitials(keyboardState);
                break;
        }

        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }

    private void UpdateTitle(KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space))
        {
            StartNewGame();
        }
        if (keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
        {
            StartNewGame();
        }
    }

    private void StartNewGame()
    {
        _gameStateManager.StartGame();
        _player.Reset();
        _playerBullet.Deactivate();
        _waveManager.Reset();
        _waveManager.StartNextWave();
        _waveTransitionTimer = 0;
        _waveCompleteTriggered = false;
    }

    private void UpdatePlaying(GameTime gameTime, KeyboardState keyboardState)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Check for wave transition
        if (_waveManager.WaveComplete)
        {
            if (!_waveCompleteTriggered)
            {
                _soundManager.PlayWaveComplete();
                _waveCompleteTriggered = true;
            }

            _waveTransitionTimer += deltaTime;
            if (_waveTransitionTimer >= WaveTransitionDelay)
            {
                _gameStateManager.AwardWaveClearBonus(_waveManager.CurrentWave);
                _waveManager.StartNextWave();
                _waveTransitionTimer = 0;
                _waveCompleteTriggered = false;
            }
        }

        // Update player
        _player.Update(gameTime, keyboardState);

        // Handle shooting
        if ((keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)) ||
            (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up)) ||
            (keyboardState.IsKeyDown(Keys.W) && _previousKeyboardState.IsKeyUp(Keys.W)))
        {
            if (!_playerBullet.IsActive)
            {
                _playerBullet.Fire(new Vector2(_player.Position.X, _player.Position.Y - 15));
                _soundManager.PlayShoot();
            }
        }

        // Update bullet
        _playerBullet.Update(gameTime);

        // Update demons
        bool demonFired = _waveManager.Update(gameTime, _player.Position);
        if (demonFired)
        {
            _soundManager.PlayDemonShoot();
        }

        // Check collisions
        CheckCollisions();
    }

    private void CheckCollisions()
    {
        // Player bullet vs demons
        var hitDemon = CollisionManager.CheckBulletVsDemons(_playerBullet, _waveManager.Demons);
        if (hitDemon != null)
        {
            _playerBullet.Deactivate();
            _gameStateManager.AddScore(hitDemon.GetPoints());
            _soundManager.PlayExplosion();

            if (hitDemon.ShouldSplit())
            {
                _waveManager.SpawnSplitDemons(hitDemon);
            }

            hitDemon.Destroy();
        }

        // Demon bullets vs player
        var hitBullet = CollisionManager.CheckDemonBulletsVsPlayer(_waveManager.DemonBullets, _player);
        if (hitBullet != null)
        {
            hitBullet.Deactivate();
            HandlePlayerHit();
        }

        // Demons vs player (collision)
        var collidedDemon = CollisionManager.CheckDemonsVsPlayer(_waveManager.Demons, _player);
        if (collidedDemon != null)
        {
            HandlePlayerHit();
        }
    }

    private void HandlePlayerHit()
    {
        _soundManager.PlayPlayerHit();
        bool gameOver = _gameStateManager.LoseLife();
        if (gameOver)
        {
            _soundManager.PlayGameOver();
        }
        else
        {
            _player.Reset();
            _player.TriggerInvincibility();
            _playerBullet.Deactivate();
        }
    }

    private void UpdateGameOver(KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
        {
            // If it's a high score, enter initials first before allowing restart
            if (_gameStateManager.HighScoreManager.IsHighScore(_gameStateManager.Score))
            {
                _gameStateManager.CheckAndEnterHighScore();
            }
            else
            {
                StartNewGame();
            }
        }
        if (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space))
        {
            _gameStateManager.CheckAndEnterHighScore();
        }
    }

    private void UpdateEnteringInitials(KeyboardState keyboardState)
    {
        // Handle letter keys A-Z
        for (Keys key = Keys.A; key <= Keys.Z; key++)
        {
            if (keyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key))
            {
                char letter = (char)('A' + (key - Keys.A));
                _gameStateManager.SetCurrentInitial(letter);
                // Auto-advance cursor unless at last position
                if (_gameStateManager.InitialsCursorPosition < 2)
                {
                    _gameStateManager.MoveCursorRight();
                }
            }
        }

        // Navigate with arrow keys
        if (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboardState.IsKeyUp(Keys.Up))
        {
            _gameStateManager.CycleInitialUp();
        }
        if (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboardState.IsKeyUp(Keys.Down))
        {
            _gameStateManager.CycleInitialDown();
        }
        if (keyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left))
        {
            _gameStateManager.MoveCursorLeft();
        }
        if (keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right))
        {
            _gameStateManager.MoveCursorRight();
        }

        // Backspace to go back
        if (keyboardState.IsKeyDown(Keys.Back) && _previousKeyboardState.IsKeyUp(Keys.Back))
        {
            if (_gameStateManager.InitialsCursorPosition > 0)
            {
                _gameStateManager.MoveCursorLeft();
            }
            _gameStateManager.SetCurrentInitial('A');
        }

        // Enter to submit and go to title
        if (keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
        {
            _gameStateManager.SubmitHighScore();
        }

        // Space to submit and play again
        if (keyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space))
        {
            _gameStateManager.SubmitHighScore();
            StartNewGame();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        switch (_gameStateManager.CurrentState)
        {
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Playing:
                DrawPlaying();
                break;
            case GameState.GameOver:
                DrawGameOver();
                break;
            case GameState.EnteringInitials:
                DrawEnteringInitials();
                break;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawTitle()
    {
        // Title
        DrawCenteredText("DEMON ATTACK", 50, Color.Red, 3);

        // Instructions
        DrawCenteredText("Press SPACE or ENTER to Start", 120, Color.White, 1);

        var scores = _gameStateManager.HighScoreManager.Scores;
        if (scores.Count > 0)
        {
            // Two-column layout: Controls on left, High Scores on right
            int leftColumnX = 50;
            int rightColumnX = ScreenWidth / 2 + 50;
            int startY = 170;

            // Left column: Controls
            DrawText("CONTROLS:", leftColumnX, startY, Color.Cyan, 1);
            DrawText("LEFT/RIGHT or A/D - Move", leftColumnX, startY + 30, Color.Gray, 1);
            DrawText("SPACE or W - Fire", leftColumnX, startY + 55, Color.Gray, 1);
            DrawText("ESC - Quit", leftColumnX, startY + 80, Color.Gray, 1);

            // Right column: High Scores
            DrawText("HIGH SCORES:", rightColumnX, startY, Color.Yellow, 1);

            // Header row
            DrawText("RANK", rightColumnX, startY + 30, Color.Gray, 1);
            DrawText("NAME", rightColumnX + 60, startY + 30, Color.Gray, 1);
            DrawText("SCORE", rightColumnX + 120, startY + 30, Color.Gray, 1);
            DrawText("DATE", rightColumnX + 200, startY + 30, Color.Gray, 1);

            // Score rows
            for (int i = 0; i < scores.Count; i++)
            {
                int rowY = startY + 55 + i * 25;
                Color rowColor = i == 0 ? Color.Yellow : Color.White;

                DrawText($"{i + 1}.", rightColumnX, rowY, rowColor, 1);
                DrawText(scores[i].Initials, rightColumnX + 60, rowY, rowColor, 1);
                DrawText($"{scores[i].Score}", rightColumnX + 120, rowY, rowColor, 1);
                DrawText(scores[i].Date.ToString("MM/dd/yy"), rightColumnX + 200, rowY, rowColor, 1);
            }
        }
        else
        {
            // No high scores yet - centered controls
            DrawCenteredText("Controls:", ScreenHeight / 2 + 60, Color.Gray, 1);
            DrawCenteredText("LEFT/RIGHT or A/D - Move", ScreenHeight / 2 + 90, Color.Gray, 1);
            DrawCenteredText("SPACE or W - Fire", ScreenHeight / 2 + 120, Color.Gray, 1);
            DrawCenteredText("ESC - Quit", ScreenHeight / 2 + 150, Color.Gray, 1);
        }
    }

    private void DrawPlaying()
    {
        // Draw game objects
        _player.Draw(_spriteBatch);
        _playerBullet.Draw(_spriteBatch);
        _waveManager.Draw(_spriteBatch);

        // Draw HUD
        DrawHUD();

        // Draw wave complete message
        if (_waveManager.WaveComplete)
        {
            DrawCenteredText($"WAVE {_waveManager.CurrentWave} COMPLETE!", ScreenHeight / 2, Color.Yellow, 2);
        }
    }

    private void DrawHUD()
    {
        // Score (top left)
        DrawText($"SCORE: {_gameStateManager.Score}", 10, 10, Color.White, 1);

        // Wave (top center)
        DrawCenteredText($"WAVE {_waveManager.CurrentWave}", 10, Color.Cyan, 1);

        // Lives (top right)
        DrawText($"LIVES: {_gameStateManager.Lives}", ScreenWidth - 100, 10, Color.Green, 1);

        // Draw reserve bunkers (visual lives) at bottom left - miniature cannons
        for (int i = 0; i < _gameStateManager.Lives - 1; i++)
        {
            float bunkerX = 30 + i * 35;
            float bunkerY = ScreenHeight - 20;

            // Draw mini cannon base
            ShapeRenderer.DrawRectangle(_spriteBatch,
                bunkerX - 12, bunkerY - 3,
                24, 6,
                Color.Green);

            // Draw mini cannon turret
            ShapeRenderer.DrawRectangle(_spriteBatch,
                bunkerX - 3, bunkerY - 8,
                6, 6,
                Color.LightGreen);
        }
    }

    private void DrawGameOver()
    {
        DrawCenteredText("GAME OVER", ScreenHeight / 3, Color.Red, 3);
        DrawCenteredText($"Final Score: {_gameStateManager.Score}", ScreenHeight / 2, Color.White, 2);
        DrawCenteredText($"Wave Reached: {_waveManager.CurrentWave}", ScreenHeight / 2 + 50, Color.Cyan, 1);

        if (_gameStateManager.HighScoreManager.IsHighScore(_gameStateManager.Score))
        {
            DrawCenteredText("NEW HIGH SCORE!", ScreenHeight / 2 + 100, Color.Yellow, 2);
            DrawCenteredText("Press ENTER to Enter Your Initials", ScreenHeight - 80, Color.White, 1);
        }
        else
        {
            DrawCenteredText("Press ENTER to Play Again", ScreenHeight - 100, Color.White, 1);
            DrawCenteredText("Press SPACE for Title Screen", ScreenHeight - 60, Color.Gray, 1);
        }
    }

    private void DrawEnteringInitials()
    {
        // Title
        DrawCenteredText("NEW HIGH SCORE!", ScreenHeight / 4, Color.Yellow, 3);

        // Score display
        DrawCenteredText($"Score: {_gameStateManager.Score}", ScreenHeight / 4 + 60, Color.White, 2);

        // Instructions
        DrawCenteredText("Enter Your Initials", ScreenHeight / 2 - 40, Color.Cyan, 1);

        // Draw the three initial boxes
        int boxWidth = 40;
        int boxHeight = 50;
        int boxSpacing = 20;
        int totalWidth = 3 * boxWidth + 2 * boxSpacing;
        int startX = (ScreenWidth - totalWidth) / 2;
        int boxY = ScreenHeight / 2;

        for (int i = 0; i < 3; i++)
        {
            int boxX = startX + i * (boxWidth + boxSpacing);
            bool isSelected = i == _gameStateManager.InitialsCursorPosition;

            // Draw box border
            Color boxColor = isSelected ? Color.Yellow : Color.Gray;
            ShapeRenderer.DrawRectangle(_spriteBatch, boxX, boxY, boxWidth, boxHeight, boxColor);
            ShapeRenderer.DrawRectangle(_spriteBatch, boxX + 2, boxY + 2, boxWidth - 4, boxHeight - 4, Color.Black);

            // Draw the letter
            char letter = _gameStateManager.PlayerInitials[i];
            int letterX = boxX + (boxWidth - 6 * 2) / 2;  // Center the letter (6*scale width)
            int letterY = boxY + (boxHeight - 8 * 2) / 2; // Center vertically
            DrawChar(letter, letterX, letterY, isSelected ? Color.Yellow : Color.White, 2);

            // Draw up/down arrows for selected box
            if (isSelected)
            {
                // Up arrow (points up)
                int arrowX = boxX + boxWidth / 2;
                int arrowUpY = boxY - 20;
                ShapeRenderer.DrawInvertedTriangle(_spriteBatch,
                    new Vector2(arrowX, arrowUpY + 12),
                    16, 12,
                    Color.Yellow);

                // Down arrow (points down)
                int arrowDownY = boxY + boxHeight + 8;
                ShapeRenderer.DrawTriangle(_spriteBatch,
                    new Vector2(arrowX, arrowDownY),
                    16, 12,
                    Color.Yellow);
            }
        }

        // Instructions at bottom
        DrawCenteredText("Type A-Z or use UP/DOWN to change letter", ScreenHeight - 140, Color.Gray, 1);
        DrawCenteredText("LEFT/RIGHT to move cursor - BACKSPACE to clear", ScreenHeight - 110, Color.Gray, 1);
        DrawCenteredText("ENTER - Save and View Scores", ScreenHeight - 70, Color.White, 1);
        DrawCenteredText("SPACE - Save and Play Again", ScreenHeight - 40, Color.Cyan, 1);
    }

    private void DrawText(string text, int x, int y, Color color, int scale)
    {
        // Draw text using rectangles for each character (simple bitmap font)
        int charWidth = 6 * scale;
        int charHeight = 8 * scale;
        int spacing = 1 * scale;

        for (int i = 0; i < text.Length; i++)
        {
            DrawChar(text[i], x + i * (charWidth + spacing), y, color, scale);
        }
    }

    private void DrawCenteredText(string text, int y, Color color, int scale)
    {
        int charWidth = 6 * scale;
        int spacing = 1 * scale;
        int totalWidth = text.Length * (charWidth + spacing) - spacing;
        int x = (ScreenWidth - totalWidth) / 2;
        DrawText(text, x, y, color, scale);
    }

    private void DrawChar(char c, int x, int y, Color color, int scale)
    {
        // Simple 5x7 bitmap font patterns
        byte[] pattern = GetCharPattern(c);
        if (pattern.Length == 0) return;

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if ((pattern[row] & (1 << (4 - col))) != 0)
                {
                    ShapeRenderer.DrawRectangle(_spriteBatch,
                        x + col * scale, y + row * scale,
                        scale, scale, color);
                }
            }
        }
    }

    private byte[] GetCharPattern(char c)
    {
        return c switch
        {
            'A' => new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'B' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
            'C' => new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
            'D' => new byte[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
            'E' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
            'F' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
            'G' => new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0E },
            'H' => new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'I' => new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
            'J' => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
            'K' => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            'L' => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
            'M' => new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
            'N' => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            'O' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'P' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
            'Q' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
            'R' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
            'S' => new byte[] { 0x0E, 0x11, 0x10, 0x0E, 0x01, 0x11, 0x0E },
            'T' => new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            'U' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'V' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
            'W' => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A },
            'X' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
            'Y' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
            'Z' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
            '0' => new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
            '1' => new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
            '2' => new byte[] { 0x0E, 0x11, 0x01, 0x0E, 0x10, 0x10, 0x1F },
            '3' => new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x01, 0x11, 0x0E },
            '4' => new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
            '5' => new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
            '6' => new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
            '7' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            '8' => new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
            '9' => new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
            ':' => new byte[] { 0x00, 0x04, 0x04, 0x00, 0x04, 0x04, 0x00 },
            '-' => new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
            '/' => new byte[] { 0x01, 0x02, 0x02, 0x04, 0x08, 0x08, 0x10 },
            ' ' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            '!' => new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 },
            '.' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 },
            ',' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x08 },
            '?' => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
            _ => Array.Empty<byte>()
        };
    }
}
