using Microsoft.Xna.Framework;
using DemonAttackClone.Entities;

namespace DemonAttackClone.Managers;

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
