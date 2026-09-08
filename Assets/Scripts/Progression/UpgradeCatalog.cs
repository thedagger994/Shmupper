using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    public class UpgradeDef
    {
        public UpgradeId Id;
        public string Name;
        public string Blurb;
        public int MaxLevel;
        public int BaseCost;
        public int CostStep;
        public Color Tint;

        public int CostAt(int level) => BaseCost + CostStep * level;
    }

    /// The shop. Every purchase is deliberately double-edged: it makes the Gunmage stronger and
    /// raises the Keep's threat level by one, which scales every enemy that spawns afterwards.
    /// That tension is the whole game, so the catalogue is kept small enough to read at a glance
    /// during a ten second shrine visit.
    public static class UpgradeCatalog
    {
        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef
            {
                Id = UpgradeId.Vitality, Name = "GUNMAGE VITALITY",
                Blurb = "+25 MAX HEALTH, FULLY RESTORED",
                MaxLevel = 6, BaseCost = 120, CostStep = 90, Tint = Palette.HudBlood
            },
            new UpgradeDef
            {
                Id = UpgradeId.SharpenedRunes, Name = "SHARPENED RUNES",
                Blurb = "+18% WEAPON DAMAGE",
                MaxLevel = 6, BaseCost = 150, CostStep = 120, Tint = Palette.HudGold
            },
            new UpgradeDef
            {
                Id = UpgradeId.RapidSigils, Name = "RAPID SIGILS",
                Blurb = "+12% FIRE RATE",
                MaxLevel = 5, BaseCost = 140, CostStep = 110, Tint = Palette.FluxShot
            },
            new UpgradeDef
            {
                Id = UpgradeId.Quicksilver, Name = "QUICKSILVER BOOTS",
                Blurb = "+9% MOVE SPEED, HIGHER JUMP",
                MaxLevel = 5, BaseCost = 110, CostStep = 80, Tint = Palette.HudMint
            },
            new UpgradeDef
            {
                Id = UpgradeId.RuneFlask, Name = "RUNE FLASK",
                Blurb = "+1 HEALING FLASK SLOT",
                MaxLevel = 4, BaseCost = 130, CostStep = 100, Tint = Palette.ShrineGlow
            },
            new UpgradeDef
            {
                Id = UpgradeId.DeepReserves, Name = "DEEP RESERVES",
                Blurb = "+40% AMMO CAPACITY, REFILLED",
                MaxLevel = 4, BaseCost = 100, CostStep = 70, Tint = Palette.EmberShot
            },
            new UpgradeDef
            {
                Id = UpgradeId.Siphon, Name = "SOUL SIPHON",
                Blurb = "HEAL 3 HP PER KILL",
                MaxLevel = 4, BaseCost = 190, CostStep = 150, Tint = Palette.Wizard
            },
            new UpgradeDef
            {
                Id = UpgradeId.Warding, Name = "WARDING PLATE",
                Blurb = "-11% DAMAGE TAKEN",
                MaxLevel = 5, BaseCost = 170, CostStep = 130, Tint = Palette.HollowKnight
            }
        };

        public static UpgradeDef Get(UpgradeId id) => All.Find(u => u.Id == id);
    }
}
