using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DemonAttackClone.Utils;

namespace DemonAttackClone.Entities;

public class Player
{
    public Vector2 Position { get; private set; }
    public Rectangle Bounds => new((int)Position.X - Width/2, (int)Position.Y - Height/2, Width, Height);
    public bool IsInvincible => _invincibilityTimer > 0;

    private const int Width = 40;
    private const int Height = 20;
    private const float Speed = 300f;
    private const float InvincibilityDuration = 2f;

    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private float _invincibilityTimer;

    public Player(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        Reset();
    }

    public void Reset()
    {
        Position = new Vector2(_screenWidth / 2f, _screenHeight - 50);
        _invincibilityTimer = 0;
    }

    public void TriggerInvincibility()
    {
        _invincibilityTimer = InvincibilityDuration;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update invincibility
        if (_invincibilityTimer > 0)
        {
            _invincibilityTimer -= deltaTime;
        }

        // Handle movement
        float moveX = 0;
        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
        {
            moveX = -Speed * deltaTime;
        }
        if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
        {
            moveX = Speed * deltaTime;
        }

        // Apply movement with bounds checking
        float newX = Position.X + moveX;
        newX = Math.Clamp(newX, Width/2f, _screenWidth - Width/2f);
        Position = new Vector2(newX, Position.Y);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Blink when invincible
        if (IsInvincible && ((int)(_invincibilityTimer * 10) % 2 == 0))
        {
            return;
        }

        // Draw cannon base (rectangle)
        ShapeRenderer.DrawRectangle(spriteBatch,
            Position.X - Width/2, Position.Y - Height/4,
            Width, Height/2,
            Color.Green);

        // Draw cannon turret (smaller rectangle on top)
        ShapeRenderer.DrawRectangle(spriteBatch,
            Position.X - 5, Position.Y - Height/2 - 5,
            10, 10,
            Color.LightGreen);
    }
}
