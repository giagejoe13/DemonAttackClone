using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DemonAttackClone.Utils;

namespace DemonAttackClone.Entities;

public class DemonBullet
{
    public Vector2 Position { get; private set; }
    public bool IsActive { get; private set; }
    public Rectangle Bounds => new((int)Position.X - Width/2, (int)Position.Y - Height/2, Width, Height);

    private const int Width = 6;
    private const int Height = 10;

    private Vector2 _velocity;
    private readonly int _screenHeight;

    public DemonBullet(int screenHeight)
    {
        _screenHeight = screenHeight;
        IsActive = false;
    }

    public void Fire(Vector2 startPosition, Vector2 targetPosition, float speed)
    {
        Position = startPosition;

        // Calculate direction toward target
        Vector2 direction = targetPosition - startPosition;
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
        else
        {
            direction = new Vector2(0, 1); // Default downward
        }

        _velocity = direction * speed;
        IsActive = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += _velocity * deltaTime;

        // Deactivate if off screen
        if (Position.Y > _screenHeight + Height ||
            Position.X < -Width ||
            Position.X > 800 + Width)
        {
            IsActive = false;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;

        ShapeRenderer.DrawRectangle(spriteBatch,
            Position.X - Width/2, Position.Y - Height/2,
            Width, Height,
            Color.Red);
    }
}
