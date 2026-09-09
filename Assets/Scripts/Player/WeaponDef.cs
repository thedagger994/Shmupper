using UnityEngine;

namespace Shmupper
{
    /// The arsenal is a gunsmith's idea of magic: every weapon has the cadence and recoil of a
    /// firearm and the payload of a spell. Damage numbers are tuned so each one owns a range
    /// band, which is what keeps weapon switching interesting instead of optional.
    public class WeaponDef
    {
        public WeaponId Id;
        public string Name;
        public string ShortName;
        public bool Hitscan;
        public float Damage;
        public int Pellets;
        public float ShotsPerSecond;
        public float SpreadDegrees;
        public float Range;
        public int MagazineBase;
        public bool InfiniteAmmo;
        public int AmmoPerPickup;
        public float ProjectileSpeed;
        public float SplashRadius;
        public float Knockback;
        public float RecoilKick;
        public Color Tint;
        public SfxId Sound;
        public int UnlockFloor;
        public string Flavour;

        public static readonly WeaponDef[] All =
        {
            new WeaponDef
            {
                Id = WeaponId.Spellslinger, Name = "SPELLSLINGER", ShortName = "SLINGER",
                Hitscan = true, Damage = 30f, Pellets = 1, ShotsPerSecond = 4.6f,
                SpreadDegrees = 0.35f, Range = 140f, InfiniteAmmo = true, MagazineBase = 0,
                Knockback = 2f, RecoilKick = 1.6f, Tint = Palette.PlayerBolt,
                Sound = SfxId.ShootPistol, UnlockFloor = 1,
                Flavour = "SIX RUNES. NEVER EMPTY."
            },
            new WeaponDef
            {
                Id = WeaponId.Emberlance, Name = "EMBERLANCE", ShortName = "EMBER",
                Hitscan = true, Damage = 14f, Pellets = 9, ShotsPerSecond = 1.35f,
                SpreadDegrees = 5.2f, Range = 45f, MagazineBase = 36, AmmoPerPickup = 6,
                Knockback = 5f, RecoilKick = 4.5f, Tint = Palette.EmberShot,
                Sound = SfxId.ShootShotgun, UnlockFloor = 1,
                Flavour = "NINE EMBERS, ONE BREATH."
            },
            new WeaponDef
            {
                Id = WeaponId.Arcanoflux, Name = "ARCANOFLUX", ShortName = "FLUX",
                Hitscan = true, Damage = 11f, Pellets = 1, ShotsPerSecond = 11f,
                SpreadDegrees = 2.4f, Range = 90f, MagazineBase = 260, AmmoPerPickup = 40,
                Knockback = 0.6f, RecoilKick = 0.7f, Tint = Palette.FluxShot,
                Sound = SfxId.ShootFlux, UnlockFloor = 2,
                Flavour = "A STORM YOU CAN HOLD."
            },
            new WeaponDef
            {
                Id = WeaponId.Runeblaster, Name = "RUNEBLASTER", ShortName = "RUNE",
                Hitscan = false, Damage = 125f, Pellets = 1, ShotsPerSecond = 0.95f,
                SpreadDegrees = 0f, Range = 140f, MagazineBase = 18, AmmoPerPickup = 3,
                ProjectileSpeed = 34f, SplashRadius = 5.5f,
                Knockback = 12f, RecoilKick = 6f, Tint = Palette.RuneShot,
                Sound = SfxId.ShootRocket, UnlockFloor = 3,
                Flavour = "SIEGEWORK, HELD IN ONE HAND."
            }
        };

        public static WeaponDef Get(WeaponId id) => System.Array.Find(All, w => w.Id == id);
    }

    public class WeaponRuntime
    {
        public WeaponDef Def;
        public int Ammo;
        public bool Unlocked;
        public float NextShotTime;

        public WeaponRuntime(WeaponDef def)
        {
            Def = def;
            Unlocked = def.UnlockFloor <= 1;
            Ammo = def.MagazineBase;
        }

        public int MaxAmmo(RunState run) => Mathf.RoundToInt(Def.MagazineBase * run.AmmoMul);

        public bool HasAmmo => Def.InfiniteAmmo || Ammo > 0;

        public void Refill(RunState run) => Ammo = MaxAmmo(run);

        public void AddAmmo(RunState run, int amount) => Ammo = Mathf.Min(MaxAmmo(run), Ammo + amount);
    }
}
