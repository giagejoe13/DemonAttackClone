using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DemonAttackClone.Utils;

namespace DemonAttackClone.Entities;

public enum DemonSize
{
    Large,
    Small
}

public enum DemonType
{
    Classic,      // Original demon style
    Bat,          // Wider wings, smaller body
    Moth,         // Large fuzzy wings
    Dragon,       // Sharp angular wings
    Phoenix       // Flame-like wings
}

public enum WingStyle
{
    Normal,       // Standard curved wings
    Jagged,       // Sharp pointed wings
    Feathered,    // Layered feather-like wings
    Membrane,     // Bat-like membrane wings
    Flame         // Fiery wavy wings
}

public class Demon
{
    public Vector2 Position { get; private set; }
    public bool IsActive { get; private set; }
    public DemonSize Size { get; private set; }
    public DemonType Type { get; private set; }
    public WingStyle Wings { get; private set; }

    // Wing state - can be destroyed independently
    public bool HasLeftWing { get; private set; } = true;
    public bool HasRightWing { get; private set; } = true;

    public Rectangle Bounds => new(
        (int)Position.X - GetWidth()/2,
        (int)Position.Y - GetHeight()/2,
        GetWidth(),
        GetHeight());

    public Rectangle LeftWingBounds
    {
        get
        {
            float scale = Size == DemonSize.Large ? 1f : 0.65f;
            float wingWidth = 22 * scale;
            float wingHeight = 18 * scale;
            return new Rectangle(
                (int)(Position.X - 12 * scale - wingWidth),
                (int)(Position.Y - 6 * scale),
                (int)wingWidth,
                (int)wingHeight);
        }
    }

    public Rectangle RightWingBounds
    {
        get
        {
            float scale = Size == DemonSize.Large ? 1f : 0.65f;
            float wingWidth = 22 * scale;
            float wingHeight = 18 * scale;
            return new Rectangle(
                (int)(Position.X + 12 * scale),
                (int)(Position.Y - 6 * scale),
                (int)wingWidth,
                (int)wingHeight);
        }
    }

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

    public Demon(int screenWidth, Color color, DemonType type = DemonType.Classic, WingStyle wings = WingStyle.Normal)
    {
        _screenWidth = screenWidth;
        _color = color;
        Type = type;
        Wings = wings;
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

        // Reset wings
        HasLeftWing = true;
        HasRightWing = true;
    }

    public int GetWidth() => Size == DemonSize.Large ? 40 : 25;
    public int GetHeight() => Size == DemonSize.Large ? 30 : 20;
    public int GetPoints() => Size == DemonSize.Large ? 20 : 10;
    public int GetWingPoints() => Size == DemonSize.Large ? 10 : 5;

    public bool ShouldSplit() => Size == DemonSize.Large;

    public void DestroyLeftWing()
    {
        HasLeftWing = false;
    }

    public void DestroyRightWing()
    {
        HasRightWing = false;
    }

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

        // === HORNS (vary by type) ===
        DrawHorns(spriteBatch, scale, darkColor);

        // === WINGS (only if not destroyed) ===
        float wingWidth = GetWingWidth() * scale;
        float wingHeight = GetWingHeight() * scale;

        if (HasLeftWing)
        {
            DrawWing(spriteBatch, true, scale, wingFlap, bodyColor * 0.7f);
        }

        if (HasRightWing)
        {
            DrawWing(spriteBatch, false, scale, wingFlap, bodyColor * 0.7f);
        }

        // === MAIN BODY (varies by type) ===
        DrawBody(spriteBatch, scale, bodyColor, darkColor, brightColor, pulse);

        // === HEAD/FACE ===
        DrawFace(spriteBatch, scale, bodyColor);

        // === CLAWS/TALONS ===
        DrawClaws(spriteBatch, scale, darkColor);

        // === TAIL (varies by type) ===
        DrawTail(spriteBatch, scale, bodyColor);
    }

    private float GetWingWidth() => Type switch
    {
        DemonType.Bat => 28f,
        DemonType.Moth => 30f,
        DemonType.Dragon => 24f,
        DemonType.Phoenix => 26f,
        _ => 22f
    };

    private float GetWingHeight() => Type switch
    {
        DemonType.Bat => 20f,
        DemonType.Moth => 24f,
        DemonType.Dragon => 16f,
        DemonType.Phoenix => 20f,
        _ => 18f
    };

    private void DrawHorns(SpriteBatch spriteBatch, float scale, Color darkColor)
    {
        float hornHeight, hornWidth, hornAngle;

        switch (Type)
        {
            case DemonType.Bat:
                // Tall pointed ears
                hornHeight = 16 * scale;
                hornWidth = 5 * scale;
                hornAngle = 0.3f;
                break;
            case DemonType.Moth:
                // Antennae
                hornHeight = 18 * scale;
                hornWidth = 2 * scale;
                hornAngle = 0.5f;
                // Draw feathery tips
                ShapeRenderer.DrawEllipse(spriteBatch,
                    new Vector2(Position.X - 12 * scale, Position.Y - 24 * scale),
                    4 * scale, 3 * scale, darkColor);
                ShapeRenderer.DrawEllipse(spriteBatch,
                    new Vector2(Position.X + 12 * scale, Position.Y - 24 * scale),
                    4 * scale, 3 * scale, darkColor);
                break;
            case DemonType.Dragon:
                // Multiple small horns
                hornHeight = 10 * scale;
                hornWidth = 3 * scale;
                hornAngle = 0.2f;
                // Extra middle horn
                ShapeRenderer.DrawSpike(spriteBatch,
                    new Vector2(Position.X, Position.Y - 14 * scale),
                    hornWidth, hornHeight * 1.2f, 0f, darkColor);
                break;
            case DemonType.Phoenix:
                // Flame crest
                hornHeight = 14 * scale;
                hornWidth = 6 * scale;
                hornAngle = 0.1f;
                // Draw flame-like crest
                for (int i = 0; i < 3; i++)
                {
                    float offset = (i - 1) * 5 * scale;
                    float height = hornHeight * (1f - MathF.Abs(i - 1) * 0.2f);
                    ShapeRenderer.DrawSpike(spriteBatch,
                        new Vector2(Position.X + offset, Position.Y - 12 * scale),
                        hornWidth * 0.6f, height, offset * 0.02f, darkColor);
                }
                return;
            default:
                hornHeight = 12 * scale;
                hornWidth = 4 * scale;
                hornAngle = 0.4f;
                break;
        }

        // Left horn
        ShapeRenderer.DrawSpike(spriteBatch,
            new Vector2(Position.X - 8 * scale, Position.Y - 12 * scale),
            hornWidth, hornHeight, -hornAngle, darkColor);

        // Right horn
        ShapeRenderer.DrawSpike(spriteBatch,
            new Vector2(Position.X + 8 * scale, Position.Y - 12 * scale),
            hornWidth, hornHeight, hornAngle, darkColor);
    }

    private void DrawWing(SpriteBatch spriteBatch, bool isLeft, float scale, float wingFlap, Color wingColor)
    {
        float wingWidth = GetWingWidth() * scale;
        float wingHeight = GetWingHeight() * scale;
        float xOffset = isLeft ? -12 * scale : 12 * scale;
        Vector2 wingPos = new Vector2(Position.X + xOffset, Position.Y - 6 * scale);

        switch (Wings)
        {
            case WingStyle.Jagged:
                DrawJaggedWing(spriteBatch, wingPos, wingWidth, wingHeight, isLeft, wingFlap, wingColor);
                break;
            case WingStyle.Feathered:
                DrawFeatheredWing(spriteBatch, wingPos, wingWidth, wingHeight, isLeft, wingFlap, wingColor);
                break;
            case WingStyle.Membrane:
                DrawMembraneWing(spriteBatch, wingPos, wingWidth, wingHeight, isLeft, wingFlap, wingColor);
                break;
            case WingStyle.Flame:
                DrawFlameWing(spriteBatch, wingPos, wingWidth, wingHeight, isLeft, wingFlap, wingColor);
                break;
            default:
                ShapeRenderer.DrawWing(spriteBatch, wingPos, wingWidth, wingHeight, isLeft, wingFlap, wingColor);
                break;
        }
    }

    private void DrawJaggedWing(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, bool flipHorizontal, float flapAngle, Color color)
    {
        float direction = flipHorizontal ? -1 : 1;
        int segments = 5;

        for (int seg = 0; seg < segments; seg++)
        {
            float segProgress = (float)seg / segments;
            float segHeight = height / segments;
            float segWidth = width * (1f - segProgress * 0.6f);
            float flapOffset = MathF.Sin(flapAngle) * (1f - segProgress) * 8f;

            // Jagged edge - each segment has a pointed tip
            Vector2 segPos = new Vector2(
                basePos.X + direction * segProgress * width * 0.6f,
                basePos.Y + seg * segHeight + flapOffset);

            // Draw segment as triangle pointing outward
            for (int i = 0; i < (int)segHeight; i++)
            {
                float lineProgress = (float)i / segHeight;
                float lineWidth = segWidth * (1f - lineProgress * 0.8f);

                ShapeRenderer.DrawRectangle(spriteBatch,
                    flipHorizontal ? segPos.X - lineWidth : segPos.X,
                    segPos.Y + i,
                    lineWidth, 2,
                    color * (1f - segProgress * 0.3f));
            }
        }
    }

    private void DrawFeatheredWing(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, bool flipHorizontal, float flapAngle, Color color)
    {
        float direction = flipHorizontal ? -1 : 1;
        int feathers = 6;

        for (int f = 0; f < feathers; f++)
        {
            float featherProgress = (float)f / feathers;
            float featherLength = height * (0.5f + featherProgress * 0.5f);
            float featherWidth = width * 0.3f * (1f - featherProgress * 0.3f);
            float flapOffset = MathF.Sin(flapAngle) * (1f - featherProgress) * 10f;

            Vector2 featherBase = new Vector2(
                basePos.X + direction * featherProgress * width * 0.4f,
                basePos.Y + f * 3 + flapOffset);

            // Draw each feather as a tapered shape
            for (int i = 0; i < (int)featherLength; i++)
            {
                float lineProgress = (float)i / featherLength;
                float lineWidth = featherWidth * (1f - lineProgress);

                ShapeRenderer.DrawRectangle(spriteBatch,
                    flipHorizontal ? featherBase.X - lineWidth - direction * i * 0.3f : featherBase.X + direction * i * 0.3f,
                    featherBase.Y + i,
                    lineWidth, 1,
                    color * (0.7f + featherProgress * 0.3f));
            }
        }
    }

    private void DrawMembraneWing(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, bool flipHorizontal, float flapAngle, Color color)
    {
        float direction = flipHorizontal ? -1 : 1;

        // Draw wing bones (3 finger-like structures)
        for (int bone = 0; bone < 3; bone++)
        {
            float boneAngle = (bone - 1) * 0.3f;
            float boneLength = height * (0.8f + bone * 0.1f);
            float flapOffset = MathF.Sin(flapAngle) * (1f - bone * 0.2f) * 8f;

            for (int i = 0; i < (int)boneLength; i++)
            {
                float progress = (float)i / boneLength;
                float xPos = basePos.X + direction * (i * 0.5f + bone * width * 0.2f);
                float yPos = basePos.Y + i + boneAngle * i + flapOffset;

                // Bone
                ShapeRenderer.DrawRectangle(spriteBatch, xPos, yPos, 2, 2, color * 1.2f);

                // Membrane between bones (semi-transparent)
                if (bone < 2)
                {
                    float membraneWidth = width * 0.25f * (1f - progress * 0.5f);
                    ShapeRenderer.DrawRectangle(spriteBatch,
                        flipHorizontal ? xPos - membraneWidth : xPos,
                        yPos, membraneWidth, 1,
                        color * 0.5f);
                }
            }
        }
    }

    private void DrawFlameWing(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, bool flipHorizontal, float flapAngle, Color color)
    {
        float direction = flipHorizontal ? -1 : 1;

        // Animated flame effect
        for (int flame = 0; flame < 8; flame++)
        {
            float flameProgress = (float)flame / 8;
            float flameHeight = height * (0.4f + MathF.Sin(_animationTime * 3 + flame) * 0.2f + flameProgress * 0.4f);
            float flameWidth = width * 0.2f * (1f - flameProgress * 0.5f);
            float flapOffset = MathF.Sin(flapAngle) * (1f - flameProgress) * 8f;
            float waveOffset = MathF.Sin(_animationTime * 5 + flame * 0.5f) * 3;

            Vector2 flameBase = new Vector2(
                basePos.X + direction * flameProgress * width * 0.5f + waveOffset * direction,
                basePos.Y + flame * 2 + flapOffset);

            // Draw flame tongue
            for (int i = 0; i < (int)flameHeight; i++)
            {
                float lineProgress = (float)i / flameHeight;
                float lineWidth = flameWidth * (1f - lineProgress * 0.9f);
                float flicker = MathF.Sin(_animationTime * 8 + i + flame) * 1.5f;

                // Color gradient from base color to orange/yellow at tip
                Color flameColor = Color.Lerp(color, Color.Orange, lineProgress * 0.7f);

                ShapeRenderer.DrawRectangle(spriteBatch,
                    flipHorizontal ? flameBase.X - lineWidth + flicker : flameBase.X + flicker,
                    flameBase.Y + i,
                    lineWidth, 1,
                    flameColor * (0.8f + flameProgress * 0.2f));
            }
        }
    }

    private void DrawBody(SpriteBatch spriteBatch, float scale, Color bodyColor, Color darkColor, Color brightColor, float pulse)
    {
        float bodyWidth, bodyHeight;

        switch (Type)
        {
            case DemonType.Bat:
                bodyWidth = 14 * scale;
                bodyHeight = 10 * scale;
                break;
            case DemonType.Moth:
                bodyWidth = 12 * scale;
                bodyHeight = 14 * scale;
                // Fuzzy body effect
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathF.PI * 2 / 8;
                    float fuzzX = MathF.Cos(angle) * 3 * scale;
                    float fuzzY = MathF.Sin(angle) * 3 * scale;
                    ShapeRenderer.DrawEllipse(spriteBatch,
                        new Vector2(Position.X + fuzzX, Position.Y + fuzzY),
                        bodyWidth * 0.8f, bodyHeight * 0.8f, bodyColor * 0.4f);
                }
                break;
            case DemonType.Dragon:
                bodyWidth = 18 * scale;
                bodyHeight = 12 * scale;
                // Scales effect
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        float scaleX = Position.X - 9 * scale + col * 6 * scale;
                        float scaleY = Position.Y - 4 * scale + row * 4 * scale;
                        ShapeRenderer.DrawEllipse(spriteBatch,
                            new Vector2(scaleX, scaleY),
                            3 * scale, 2 * scale, darkColor * 0.8f);
                    }
                }
                break;
            case DemonType.Phoenix:
                bodyWidth = 14 * scale;
                bodyHeight = 12 * scale;
                // Glowing ember effect
                ShapeRenderer.DrawEllipse(spriteBatch, Position,
                    bodyWidth + 4 * scale, bodyHeight + 4 * scale, Color.Orange * 0.3f * pulse);
                break;
            default:
                bodyWidth = 16 * scale;
                bodyHeight = 12 * scale;
                break;
        }

        // Outer body glow/shadow
        ShapeRenderer.DrawEllipse(spriteBatch, Position,
            bodyWidth + 2 * scale, bodyHeight + 2 * scale, darkColor * 0.5f);

        // Main body
        ShapeRenderer.DrawEllipse(spriteBatch, Position,
            bodyWidth, bodyHeight, bodyColor);

        // Inner body highlight
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y - 2 * scale),
            bodyWidth * 0.6f, bodyHeight * 0.5f, brightColor * 0.6f);

        // Core glow
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y),
            5 * scale, 4 * scale, Color.White * 0.3f * pulse);
    }

    private void DrawFace(SpriteBatch spriteBatch, float scale, Color bodyColor)
    {
        // Head bump
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X, Position.Y - 8 * scale),
            10 * scale, 6 * scale, bodyColor);

        // Eyes vary by type
        float eyeSpacing = Type == DemonType.Moth ? 8 * scale : 6 * scale;
        float eyeY = Position.Y - 6 * scale;
        float eyeWidth = Type == DemonType.Bat ? 6 * scale : 5 * scale;
        float eyeHeight = Type == DemonType.Bat ? 5 * scale : 4 * scale;

        Color eyeGlowColor = Type switch
        {
            DemonType.Phoenix => Color.Orange,
            DemonType.Dragon => Color.Yellow,
            _ => Color.Red
        };

        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing, eyeY),
            eyeWidth, eyeHeight, Color.White);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing, eyeY),
            eyeWidth, eyeHeight, Color.White);

        // Pupils
        float pupilOffset = MathF.Sin(_animationTime * 0.3f) * 1.5f * scale;
        float pupilSize = 2.5f * scale;

        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing + pupilOffset, eyeY),
            pupilSize, pupilSize, Color.Black);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing + pupilOffset, eyeY),
            pupilSize, pupilSize, Color.Black);

        // Eye glow
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X - eyeSpacing, eyeY),
            eyeWidth * 0.6f, eyeHeight * 0.6f, eyeGlowColor * 0.3f);
        ShapeRenderer.DrawEllipse(spriteBatch,
            new Vector2(Position.X + eyeSpacing, eyeY),
            eyeWidth * 0.6f, eyeHeight * 0.6f, eyeGlowColor * 0.3f);

        // Mouth
        float mouthY = Position.Y - 1 * scale;
        float mouthWidth = 8 * scale;

        if (Type == DemonType.Dragon)
        {
            // Dragon has larger fangs
            for (int i = 0; i < 3; i++)
            {
                float toothX = Position.X - mouthWidth/3 + i * (mouthWidth / 2);
                float toothHeight = (i == 1) ? 5 * scale : 3 * scale;

                ShapeRenderer.DrawRectangle(spriteBatch,
                    toothX, mouthY,
                    2 * scale, toothHeight,
                    Color.White * 0.9f);
            }
        }
        else
        {
            // Standard jagged mouth
            for (int i = 0; i < 5; i++)
            {
                float toothX = Position.X - mouthWidth/2 + i * (mouthWidth / 4);
                float toothHeight = (i % 2 == 0) ? 3 * scale : 1 * scale;

                ShapeRenderer.DrawRectangle(spriteBatch,
                    toothX, mouthY,
                    2 * scale, toothHeight,
                    Color.White * 0.9f);
            }
        }
    }

    private void DrawClaws(SpriteBatch spriteBatch, float scale, Color darkColor)
    {
        if (Type == DemonType.Moth) return; // Moths don't have claws

        float clawY = Position.Y + 10 * scale;
        float clawSize = Type == DemonType.Dragon ? 10 * scale : 8 * scale;

        ShapeRenderer.DrawClaw(spriteBatch,
            new Vector2(Position.X - 8 * scale, clawY),
            clawSize, true, darkColor);

        ShapeRenderer.DrawClaw(spriteBatch,
            new Vector2(Position.X + 8 * scale, clawY),
            clawSize, false, darkColor);
    }

    private void DrawTail(SpriteBatch spriteBatch, float scale, Color bodyColor)
    {
        if (Type == DemonType.Bat) return; // Bats don't have visible tails

        float tailStartY = Position.Y + 8 * scale;
        int tailLength = Type == DemonType.Dragon ? 12 : 8;

        for (int i = 0; i < tailLength; i++)
        {
            float progress = (float)i / tailLength;
            float tailX = Position.X + MathF.Sin(progress * MathF.PI + _animationTime * 0.5f) * 6 * scale;
            float tailY = tailStartY + i * 2 * scale;
            float thickness = (1f - progress * 0.8f) * 4 * scale;

            Color tailColor = bodyColor;
            if (Type == DemonType.Phoenix)
            {
                tailColor = Color.Lerp(bodyColor, Color.Orange, progress);
            }

            ShapeRenderer.DrawRectangle(spriteBatch,
                tailX - thickness/2, tailY,
                thickness, 2 * scale,
                tailColor * (1f - progress * 0.5f));
        }

        // Dragon tail spike
        if (Type == DemonType.Dragon)
        {
            ShapeRenderer.DrawTriangle(spriteBatch,
                new Vector2(Position.X, tailStartY + tailLength * 2 * scale),
                6 * scale, 8 * scale, bodyColor);
        }
    }
}
