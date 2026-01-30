using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DemonAttackClone.Utils;

namespace DemonAttackClone.Entities;

public enum DemonSize
{
    Large,
    Small
}

public class Demon
{
    public Vector2 Position { get; private set; }
    public bool IsActive { get; private set; }
    public DemonSize Size { get; private set; }
    public Rectangle Bounds => new(
        (int)Position.X - GetWidth()/2,
        (int)Position.Y - GetHeight()/2,
        GetWidth(),
        GetHeight());

    private float _baseSpeed;
    private float _horizontalDirection;
    private float _swoopTimer;
    private float _swoopCooldown;
    private bool _isSwooping;
    private float _swoopStartY;
    private float _fireTimer;
    private readonly Color _color;
    private readonly int _screenWidth;
    private readonly Random _random;

    // Animation
    private float _animationTime;
    private float _wingFlapSpeed;
    private float _pulsePhase;

    private const float SwoopDepth = 180f;  // Deeper swoops
    private const float SwoopSpeed = 280f;  // Faster swooping
    private float _homeY;  // Fixed home position to return to
    private float _oscillationPhase;  // For vertical bobbing

    public Demon(int screenWidth, Color color)
    {
        _screenWidth = screenWidth;
        _color = color;
        _random = new Random();
        IsActive = false;
    }

    public void Spawn(Vector2 position, DemonSize size, float speed, float aggressiveness)
    {
        Position = position;
        Size = size;
        _baseSpeed = speed;
        _horizontalDirection = _random.Next(2) == 0 ? 1 : -1;
        _swoopTimer = (float)_random.NextDouble() * 2f; // Stagger initial swoop timing
        _swoopCooldown = 1.5f + (float)_random.NextDouble() * 2f; // Swoop more frequently
        _swoopStartY = position.Y;
        _homeY = position.Y; // Fixed home position
        _oscillationPhase = (float)_random.NextDouble() * MathF.PI * 2;
        _isSwooping = false;
        _fireTimer = (float)_random.NextDouble() * (3f / aggressiveness);
        IsActive = true;

        // Randomize animation phase so demons don't all flap in sync
        _animationTime = (float)_random.NextDouble() * MathF.PI * 2;
        _wingFlapSpeed = 8f + (float)_random.NextDouble() * 4f;
        _pulsePhase = (float)_random.NextDouble() * MathF.PI * 2;
    }

    public int GetWidth() => Size == DemonSize.Large ? 40 : 25;
    public int GetHeight() => Size == DemonSize.Large ? 30 : 20;
    public int GetPoints() => Size == DemonSize.Large ? 20 : 10;

    public bool ShouldSplit() => Size == DemonSize.Large;

    public bool TryFire(GameTime gameTime, float aggressiveness)
    {
        if (!IsActive) return false;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fireTimer -= deltaTime;

        if (_fireTimer <= 0)
        {
            _fireTimer = (2f + (float)_random.NextDouble() * 2f) / aggressiveness;
            return true;
        }

        return false;
    }

    public void Update(GameTime gameTime, float speedMultiplier)
    {
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float currentSpeed = _baseSpeed * speedMultiplier;

        // Update animation
        _animationTime += deltaTime * _wingFlapSpeed;
        _oscillationPhase += deltaTime * 2f;

        if (_isSwooping)
        {
            // Swoop down aggressively
            float swoopProgress = (Position.Y - _swoopStartY) / SwoopDepth;

            // Move horizontally while swooping (dive-bomb behavior)
            float newX = Position.X + _horizontalDirection * currentSpeed * 0.5f * deltaTime;
            if (newX <= GetWidth()/2 || newX >= _screenWidth - GetWidth()/2)
            {
                _horizontalDirection *= -1;
                newX = Math.Clamp(newX, GetWidth()/2f, _screenWidth - GetWidth()/2f);
            }

            float newY = Position.Y + SwoopSpeed * deltaTime;

            Position = new Vector2(newX, newY);

            if (Position.Y >= _swoopStartY + SwoopDepth)
            {
                _isSwooping = false;
            }
        }
        else
        {
            // Horizontal movement
            float newX = Position.X + _horizontalDirection * currentSpeed * deltaTime;

            if (newX <= GetWidth()/2 || newX >= _screenWidth - GetWidth()/2)
            {
                _horizontalDirection *= -1;
                newX = Math.Clamp(newX, GetWidth()/2f, _screenWidth - GetWidth()/2f);
            }

            // Vertical oscillation (bobbing motion)
            float oscillation = MathF.Sin(_oscillationPhase) * 15f;
            float targetY = _homeY + oscillation;

            // Return to home position after swoop
            float newY = Position.Y;
            if (Position.Y > targetY + 5)
            {
                newY = Position.Y - SwoopSpeed * deltaTime * 0.4f;
            }
            else if (Position.Y < targetY - 5)
            {
                newY = Position.Y + SwoopSpeed * deltaTime * 0.2f;
            }
            else
            {
                newY = targetY;
            }

            Position = new Vector2(newX, newY);

            // Swoop trigger
            _swoopTimer += deltaTime;
            if (_swoopTimer >= _swoopCooldown)
            {
                _swoopTimer = 0;
                _swoopCooldown = 1.5f + (float)_random.NextDouble() * 2.5f;
                _swoopStartY = Position.Y;
                _isSwooping = true;
            }
        }
    }

    public void Destroy()
    {
        IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;

        float scale = Size == DemonSize.Large ? 1f : 0.65f;
        float wingFlap = MathF.Sin(_animationTime);
        float pulse = 0.85f + MathF.Sin(_animationTime * 0.5f + _pulsePhase) * 0.15f;

        // Colors
        Color bodyColor = _color * pulse;
        Color darkColor = new Color(
            (int)(_color.R * 0.5f),
            (int)(_color.G * 0.5f),
            (int)(_color.B * 0.5f));
        Color brightColor = new Color(
            Math.Min(255, (int)(_color.R * 1.3f)),
            Math.Min(255, (int)(_color.G * 1.3f)),
            Math.Min(255, (int)(_color.B * 1.3f)));

        // === HORNS ===
        float hornHeight = 12 * scale;
        float hornWidth = 4 * scale;

        // Left horn
        ShapeRenderer.DrawSpike(spriteBatch,
            new Vector2(Position.X - 8 * scale, Position.Y - 12 * scale),
            hornWidth, hornHeight, -0.4f, darkColor);

        // Right horn
        ShapeRenderer.DrawSpike(spriteBatch,
            new Vector2(Position.X + 8 * scale, Position.Y - 12 * scale),
            hornWidth, hornHeight, 0.4f, darkColor);

        // === WINGS ===
        float wingWidth = 22 * scale;
        float wingHeight = 18 * scale;

        // Left wing (behind body)
        ShapeRenderer.DrawWing(spriteBatch,
            new Vector2(Position.X - 12 * scale, Position.Y - 6 * scale),
            wingWidth, wingHeight, true, wingFlap, bodyColor * 0.7f);

        // Right wing (behind body)
        ShapeRenderer.DrawWing(spriteBatch,
            new Vector2(Position.X + 12 * scale, Position.Y - 6 * scale),
            wingWidth, wingHeight, false, wingFlap, bodyColor * 0.7f);

        // === MAIN BODY ===
        // Outer body glow/shadow
        ShapeRenderer.DrawEllipse(spriteBatch, Position,
            18 * scale, 14 * scale, darkColor * 0.5f);

        // Main body
        ShapeRenderer.DrawEllipse(spriteBatch, Position,
            16 * scale, 12 * scale, bodyColor);

        // Inner body highlight
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y - 2 * scale),
            10 * scale, 6 * scale, brightColor * 0.6f);

        // Core glow
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y),
            5 * scale, 4 * scale, Color.White * 0.3f * pulse);

        // === HEAD/FACE ===
        // Head bump
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y - 8 * scale),
            10 * scale, 6 * scale, bodyColor);

        // Eyes - white outer
        float eyeSpacing = 6 * scale;
        float eyeY = Position.Y - 6 * scale;
        float eyeWidth = 5 * scale;
        float eyeHeight = 4 * scale;

        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing, eyeY),
            eyeWidth, eyeHeight, Color.White);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing, eyeY),
            eyeWidth, eyeHeight, Color.White);

        // Pupils - follow a slight pattern
        float pupilOffset = MathF.Sin(_animationTime * 0.3f) * 1.5f * scale;
        float pupilSize = 2.5f * scale;

        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing + pupilOffset, eyeY),
            pupilSize, pupilSize, Color.Black);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing + pupilOffset, eyeY),
            pupilSize, pupilSize, Color.Black);

        // Eye glow (menacing red tint)
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing, eyeY),
            eyeWidth * 0.6f, eyeHeight * 0.6f, Color.Red * 0.3f);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing, eyeY),
            eyeWidth * 0.6f, eyeHeight * 0.6f, Color.Red * 0.3f);

        // === MOUTH ===
        float mouthY = Position.Y - 1 * scale;
        float mouthWidth = 8 * scale;

        // Jagged mouth line
        for (int i = 0; i < 5; i++)
        {
            float toothX = Position.X - mouthWidth/2 + i * (mouthWidth / 4);
            float toothHeight = (i % 2 == 0) ? 3 * scale : 1 * scale;

            ShapeRenderer.DrawRectangle(spriteBatch,
                toothX, mouthY,
                2 * scale, toothHeight,
                Color.White * 0.9f);
        }

        // === CLAWS/TALONS ===
        float clawY = Position.Y + 10 * scale;
        float clawSize = 8 * scale;

        ShapeRenderer.DrawClaw(spriteBatch,
            new Vector2(Position.X - 8 * scale, clawY),
            clawSize, true, darkColor);

        ShapeRenderer.DrawClaw(spriteBatch,
            new Vector2(Position.X + 8 * scale, clawY),
            clawSize, false, darkColor);

        // === TAIL ===
        float tailStartY = Position.Y + 8 * scale;
        float tailWave = MathF.Sin(_animationTime * 0.7f) * 4 * scale;

        for (int i = 0; i < 8; i++)
        {
            float progress = i / 8f;
            float tailX = Position.X + MathF.Sin(progress * MathF.PI + _animationTime * 0.5f) * 6 * scale;
            float tailY = tailStartY + i * 2 * scale;
            float thickness = (1f - progress * 0.8f) * 4 * scale;

            ShapeRenderer.DrawRectangle(spriteBatch,
                tailX - thickness/2, tailY,
                thickness, 2 * scale,
                bodyColor * (1f - progress * 0.5f));
        }
    }
}
