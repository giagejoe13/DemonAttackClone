using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DemonAttackClone.Utils;

namespace DemonAttackClone.Entities;

public class Bullet
{
    public Vector2 Position { get; private set; }
    public bool IsActive { get; private set; }
    public Rectangle Bounds => new((int)Position.X - Width/2, (int)Position.Y - Height/2, Width, Height);

    private const int Width = 4;
    private const int Height = 12;
    private const float Speed = 500f;

    public Bullet()
    {
        IsActive = false;
    }

    public void Fire(Vector2 startPosition)
    {
        Position = startPosition;
        IsActive = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position = new Vector2(Position.X, Position.Y - Speed * deltaTime);

        // Deactivate if off screen
        if (Position.Y < -Height)
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
            Color.Yellow);
    }
}
