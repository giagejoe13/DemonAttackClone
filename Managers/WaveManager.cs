using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DemonAttackClone.Entities;

namespace DemonAttackClone.Managers;

public class WaveManager
{
    public int CurrentWave { get; private set; }
    public List<Demon> Demons { get; private set; }
    public List<DemonBullet> DemonBullets { get; private set; }
    public bool WaveComplete => Demons.All(d => !d.IsActive);

    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly Random _random;

    private const int MaxDemonBullets = 10;
    private const int BaseDemonCount = 6;

    private static readonly Color[] WaveColors =
    {
        Color.Magenta,
        Color.Cyan,
        Color.Orange,
        Color.Yellow,
        Color.LimeGreen,
        Color.HotPink,
        Color.Aquamarine,
        Color.Coral
    };

    public WaveManager(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _random = new Random();
        Demons = new List<Demon>();
        DemonBullets = new List<DemonBullet>();

        // Pre-create demon bullet pool
        for (int i = 0; i < MaxDemonBullets; i++)
        {
            DemonBullets.Add(new DemonBullet(_screenHeight));
        }
    }

    public void Reset()
    {
        CurrentWave = 0;
        Demons.Clear();
        foreach (var bullet in DemonBullets)
        {
            bullet.Deactivate();
        }
    }

    public void StartNextWave()
    {
        CurrentWave++;
        SpawnWave();
    }

    private void SpawnWave()
    {
        Demons.Clear();

        Color waveColor = WaveColors[(CurrentWave - 1) % WaveColors.Length];
        int demonCount = Math.Min(BaseDemonCount + (CurrentWave - 1), 12);
        float baseSpeed = 50f + (CurrentWave - 1) * 10f;

        // Spawn demons in rows
        int demonsPerRow = Math.Min(demonCount, 6);
        int rows = (int)Math.Ceiling((float)demonCount / demonsPerRow);

        int spawned = 0;
        for (int row = 0; row < rows && spawned < demonCount; row++)
        {
            int demonsInThisRow = Math.Min(demonsPerRow, demonCount - spawned);
            float rowY = 80 + row * 60;
            float spacing = _screenWidth / (demonsInThisRow + 1);

            for (int col = 0; col < demonsInThisRow; col++)
            {
                var demon = new Demon(_screenWidth, waveColor);
                float x = spacing * (col + 1);
                demon.Spawn(new Vector2(x, rowY), DemonSize.Large, baseSpeed, GetAggressiveness());
                Demons.Add(demon);
                spawned++;
            }
        }
    }

    public float GetSpeedMultiplier()
    {
        // Speed up as fewer demons remain
        int activeDemons = Demons.Count(d => d.IsActive);
        int totalDemons = Demons.Count;
        if (totalDemons == 0) return 1f;

        float remaining = (float)activeDemons / totalDemons;
        return 1f + (1f - remaining) * 0.5f;
    }

    public float GetAggressiveness()
    {
        return 1f + (CurrentWave - 1) * 0.2f;
    }

    public void SpawnSplitDemons(Demon parent)
    {
        Color waveColor = WaveColors[(CurrentWave - 1) % WaveColors.Length];
        float baseSpeed = 60f + (CurrentWave - 1) * 15f;

        // Spawn two smaller demons
        for (int i = 0; i < 2; i++)
        {
            var demon = new Demon(_screenWidth, waveColor);
            float offsetX = (i == 0 ? -20 : 20);
            demon.Spawn(
                new Vector2(parent.Position.X + offsetX, parent.Position.Y),
                DemonSize.Small,
                baseSpeed,
                GetAggressiveness()
            );
            Demons.Add(demon);
        }
    }

    public bool Update(GameTime gameTime, Vector2 playerPosition)
    {
        float speedMultiplier = GetSpeedMultiplier();
        float aggressiveness = GetAggressiveness();
        bool demonFired = false;

        foreach (var demon in Demons)
        {
            if (!demon.IsActive) continue;

            demon.Update(gameTime, speedMultiplier);

            // Try to fire
            if (demon.TryFire(gameTime, aggressiveness))
            {
                FireDemonBullet(demon.Position, playerPosition);
                demonFired = true;
            }
        }

        // Update demon bullets
        foreach (var bullet in DemonBullets)
        {
            bullet.Update(gameTime);
        }

        return demonFired;
    }

    private void FireDemonBullet(Vector2 from, Vector2 target)
    {
        var bullet = DemonBullets.FirstOrDefault(b => !b.IsActive);
        if (bullet != null)
        {
            float bulletSpeed = 150f + CurrentWave * 20f;
            bullet.Fire(from, target, bulletSpeed);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var demon in Demons)
        {
            demon.Draw(spriteBatch);
        }

        foreach (var bullet in DemonBullets)
        {
            bullet.Draw(spriteBatch);
        }
    }
}
