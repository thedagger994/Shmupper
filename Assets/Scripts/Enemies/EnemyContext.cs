using System;
using UnityEngine;

namespace Shmupper
{
    /// Shared services every enemy needs. Passed in at spawn time rather than looked up, so no
    /// enemy ever does a scene search and the whole horde can be rebuilt between floors.
    public class EnemyContext
    {
        public Transform Player;
        public PlayerHealth PlayerHealth;
        public PlayerController PlayerController;
        public FlowField Flow;
        public RunState Run;
        public DungeonMap Map;
        public Action<Enemy> OnEnemyDied;

        /// Lets an enemy call for reinforcements without knowing anything about the wave
        /// director. Only the Lich uses it, but it keeps summoning out of the spawner.
        public Func<EnemyKind, Vector3, Enemy> SpawnEnemy;
    }

    /// Serializable so the numbers can live on the enemy prefab and be tuned in the Inspector.
    /// The table below is the source the asset forge stamps those prefabs from, and the fallback
    /// for any enemy created in code. A Health of zero means "nothing was authored here, use the
    /// table" - Unity always constructs a serialized class field, so it can never be null.
    [System.Serializable]
    public class EnemyStats
    {
        [Tooltip("Base pool before threat and depth scaling. Quoted in Spellslinger shots: 30 damage each.")]
        public float Health;
        public float Speed;
        public float Damage;
        public int Points;
        [Tooltip("Collision radius and height. Also sizes the spawn burst.")]
        public float Radius;
        public float Height;

        public bool IsAuthored => Health > 0f;

        public static EnemyStats For(EnemyKind kind)
        {
            switch (kind)
            {
                // Health is quoted in Spellslinger shots at base damage (30), because that is the
                // gun the player always has and the yardstick every other weapon is felt
                // against. Trash dies in one, mid-tier in two or three, and only the Gargoyle
                // asks the player to commit. Nothing in the castle is a bullet sponge.
                case EnemyKind.Imp:
                    return new EnemyStats { Health = 22f, Speed = 7.4f, Damage = 7f, Points = 100, Radius = 0.45f, Height = 1.2f };
                case EnemyKind.HollowKnight:
                    return new EnemyStats { Health = 68f, Speed = 4.6f, Damage = 17f, Points = 250, Radius = 0.6f, Height = 2.1f };
                case EnemyKind.Wizard:
                    return new EnemyStats { Health = 45f, Speed = 3.9f, Damage = 13f, Points = 400, Radius = 0.6f, Height = 2.0f };
                case EnemyKind.BabyDragon:
                    return new EnemyStats { Health = 58f, Speed = 6.6f, Damage = 15f, Points = 500, Radius = 0.7f, Height = 1.1f };
                case EnemyKind.Gargoyle:
                    return new EnemyStats { Health = 125f, Speed = 3.2f, Damage = 26f, Points = 750, Radius = 0.85f, Height = 2.6f };
                default:
                    return new EnemyStats { Health = 620f, Speed = 4.2f, Damage = 30f, Points = 5000, Radius = 1.1f, Height = 3.2f };
            }
        }
    }
}
