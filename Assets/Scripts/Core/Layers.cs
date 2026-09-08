using UnityEngine;

namespace Shmupper
{
    /// Layer indices are fixed here rather than read from the project settings so the game can
    /// configure its own collision matrix at boot and never depend on editor-side setup.
    public static class Layers
    {
        public const int Level      = 8;
        public const int Player     = 9;
        public const int Enemy      = 10;
        public const int Projectile = 11;
        public const int Pickup     = 12;

        public static readonly int LevelMask      = 1 << Level;
        public static readonly int PlayerMask     = 1 << Player;
        public static readonly int EnemyMask      = 1 << Enemy;
        public static readonly int ProjectileMask = 1 << Projectile;
        public static readonly int PickupMask     = 1 << Pickup;

        public static int ShotMask => LevelMask | EnemyMask;
        public static int EnemyShotMask => LevelMask | PlayerMask;
        public static int SightMask => LevelMask;

        public static void ConfigureMatrix()
        {
            Physics.IgnoreLayerCollision(Projectile, Projectile, true);
            Physics.IgnoreLayerCollision(Projectile, Pickup, true);
            Physics.IgnoreLayerCollision(Enemy, Enemy, false);
            Physics.IgnoreLayerCollision(Pickup, Enemy, true);
            Physics.IgnoreLayerCollision(Pickup, Pickup, true);
        }

        public static void Apply(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                Apply(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
