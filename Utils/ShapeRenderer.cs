using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DemonAttackClone.Utils;

public static class ShapeRenderer
{
    private static Texture2D? _pixel;

    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixel != null)
        {
            spriteBatch.Draw(_pixel, rect, color);
        }
    }

    public static void DrawRectangle(SpriteBatch spriteBatch, float x, float y, float width, float height, Color color)
    {
        DrawRectangle(spriteBatch, new Rectangle((int)x, (int)y, (int)width, (int)height), color);
    }

    public static void DrawTriangle(SpriteBatch spriteBatch, Vector2 top, float width, float height, Color color)
    {
        if (_pixel == null) return;

        int rows = (int)height;
        for (int i = 0; i < rows; i++)
        {
            float progress = (float)i / rows;
            float lineWidth = width * progress;
            float xOffset = (width - lineWidth) / 2;

            spriteBatch.Draw(_pixel,
                new Rectangle((int)(top.X - width/2 + xOffset), (int)(top.Y + i), (int)lineWidth + 1, 1),
                color);
        }
    }

    public static void DrawInvertedTriangle(SpriteBatch spriteBatch, Vector2 bottom, float width, float height, Color color)
    {
        if (_pixel == null) return;

        int rows = (int)height;
        for (int i = 0; i < rows; i++)
        {
            float progress = 1f - ((float)i / rows);
            float lineWidth = width * progress;
            float xOffset = (width - lineWidth) / 2;

            spriteBatch.Draw(_pixel,
                new Rectangle((int)(bottom.X - width/2 + xOffset), (int)(bottom.Y - height + i), (int)lineWidth + 1, 1),
                color);
        }
    }

    public static void DrawDiamond(SpriteBatch spriteBatch, Vector2 center, float width, float height, Color color)
    {
        DrawTriangle(spriteBatch, new Vector2(center.X, center.Y - height/2), width, height/2, color);
        DrawInvertedTriangle(spriteBatch, new Vector2(center.X, center.Y + height/2), width, height/2, color);
    }

    // Draw an ellipse/oval shape
    public static void DrawEllipse(SpriteBatch spriteBatch, Vector2 center, float radiusX, float radiusY, Color color)
    {
        if (_pixel == null) return;

        int rows = (int)(radiusY * 2);
        for (int i = 0; i < rows; i++)
        {
            float y = center.Y - radiusY + i;
            float normalizedY = (i - radiusY) / radiusY;
            float lineHalfWidth = radiusX * (float)Math.Sqrt(1 - normalizedY * normalizedY);

            if (lineHalfWidth > 0)
            {
                spriteBatch.Draw(_pixel,
                    new Rectangle((int)(center.X - lineHalfWidth), (int)y, (int)(lineHalfWidth * 2), 1),
                    color);
            }
        }
    }

    // Draw a wing shape (curved triangle)
    public static void DrawWing(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, bool flipHorizontal, float flapAngle, Color color)
    {
        if (_pixel == null) return;

        int rows = (int)height;
        float direction = flipHorizontal ? -1 : 1;

        for (int i = 0; i < rows; i++)
        {
            float progress = (float)i / rows;
            // Wing tapers and curves
            float curve = (float)Math.Sin(progress * Math.PI) * 0.3f;
            float taper = 1f - progress * 0.7f;
            float lineWidth = width * taper;

            // Apply flap angle offset
            float flapOffset = (float)Math.Sin(flapAngle) * (1f - progress) * 8f;

            float xPos = basePos.X + direction * (progress * width * 0.5f + curve * width);

            spriteBatch.Draw(_pixel,
                new Rectangle(
                    (int)(flipHorizontal ? xPos - lineWidth : xPos),
                    (int)(basePos.Y + i + flapOffset),
                    (int)lineWidth + 1, 2),
                color);
        }
    }

    // Draw a horn/spike
    public static void DrawSpike(SpriteBatch spriteBatch, Vector2 basePos, float width, float height, float angle, Color color)
    {
        if (_pixel == null) return;

        int length = (int)height;
        for (int i = 0; i < length; i++)
        {
            float progress = (float)i / length;
            float currentWidth = width * (1f - progress);

            float xOffset = (float)Math.Sin(angle) * i;
            float yOffset = -(float)Math.Cos(angle) * i;

            if (currentWidth >= 1)
            {
                spriteBatch.Draw(_pixel,
                    new Rectangle(
                        (int)(basePos.X + xOffset - currentWidth/2),
                        (int)(basePos.Y + yOffset),
                        (int)currentWidth + 1, 2),
                    color);
            }
        }
    }

    // Draw a claw/talon
    public static void DrawClaw(SpriteBatch spriteBatch, Vector2 basePos, float size, bool flipHorizontal, Color color)
    {
        if (_pixel == null) return;

        float direction = flipHorizontal ? -1 : 1;

        // Draw 3 curved claw segments
        for (int claw = 0; claw < 3; claw++)
        {
            float clawOffset = (claw - 1) * size * 0.4f;

            for (int i = 0; i < (int)size; i++)
            {
                float progress = (float)i / size;
                float curve = direction * (float)Math.Pow(progress, 2) * size * 0.3f;
                float thickness = (1f - progress * 0.5f) * 2;

                spriteBatch.Draw(_pixel,
                    new Rectangle(
                        (int)(basePos.X + clawOffset + curve),
                        (int)(basePos.Y + i),
                        (int)thickness + 1, 1),
                    color);
            }
        }
    }
}
