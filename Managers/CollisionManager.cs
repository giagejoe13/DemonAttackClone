using Microsoft.Xna.Framework;
using DemonAttackClone.Entities;

namespace DemonAttackClone.Managers;

public enum HitType
{
    None,
    Body,
    LeftWing,
    RightWing
}

public class HitResult
{
    public Demon? Demon { get; set; }
    public HitType HitType { get; set; }

    public HitResult(Demon? demon, HitType hitType)
    {
        Demon = demon;
        HitType = hitType;
    }
}

public class CollisionManager
{
    public static bool CheckCollision(Rectangle a, Rectangle b)
    {
        return a.Intersects(b);
    }

    public static Demon? CheckBulletVsDemons(Bullet bullet, List<Demon> demons)
    {
        if (!bullet.IsActive) return null;

        foreach (var demon in demons)
        {
            if (demon.IsActive && CheckCollision(bullet.Bounds, demon.Bounds))
            {
                return demon;
            }
        }

        return null;
    }

    public static HitResult CheckBulletVsDemonsWithWings(Bullet bullet, List<Demon> demons)
    {
        if (!bullet.IsActive) return new HitResult(null, HitType.None);

        foreach (var demon in demons)
        {
            if (!demon.IsActive) continue;

            // Check body first (center hit)
            if (CheckCollision(bullet.Bounds, demon.Bounds))
            {
                return new HitResult(demon, HitType.Body);
            }

            // Check left wing (if still attached)
            if (demon.HasLeftWing && CheckCollision(bullet.Bounds, demon.LeftWingBounds))
            {
                return new HitResult(demon, HitType.LeftWing);
            }

            // Check right wing (if still attached)
            if (demon.HasRightWing && CheckCollision(bullet.Bounds, demon.RightWingBounds))
            {
                return new HitResult(demon, HitType.RightWing);
            }
        }

        return new HitResult(null, HitType.None);
    }

    public static DemonBullet? CheckDemonBulletsVsPlayer(List<DemonBullet> bullets, Player player)
    {
        if (player.IsInvincible) return null;

        foreach (var bullet in bullets)
        {
            if (bullet.IsActive && CheckCollision(bullet.Bounds, player.Bounds))
            {
                return bullet;
            }
        }

        return null;
    }

    public static Demon? CheckDemonsVsPlayer(List<Demon> demons, Player player)
    {
        if (player.IsInvincible) return null;

        foreach (var demon in demons)
        {
            if (demon.IsActive && CheckCollision(demon.Bounds, player.Bounds))
            {
                return demon;
            }
        }

        return null;
    }
}
