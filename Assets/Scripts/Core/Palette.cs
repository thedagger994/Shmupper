using UnityEngine;

namespace Shmupper
{
    /// Central colour vocabulary. Every enemy, weapon and UI element pulls from here so the
    /// game reads as one coherent piece: cold stone world, warm player magic, and one saturated
    /// signature hue per enemy family so silhouettes are identifiable at a glance.
    public static class Palette
    {
        public static readonly Color StoneFloor  = new Color(0.30f, 0.29f, 0.33f);
        public static readonly Color StoneWall   = new Color(0.38f, 0.36f, 0.40f);
        public static readonly Color StoneTrim   = new Color(0.22f, 0.21f, 0.26f);
        public static readonly Color Ceiling     = new Color(0.12f, 0.12f, 0.16f);
        public static readonly Color Pillar      = new Color(0.44f, 0.42f, 0.46f);

        public static readonly Color TorchLight  = new Color(1.00f, 0.62f, 0.26f);
        public static readonly Color ArcaneLight = new Color(0.42f, 0.55f, 1.00f);
        public static readonly Color ShrineGlow  = new Color(0.45f, 1.00f, 0.85f);

        public static readonly Color Imp         = new Color(0.45f, 0.95f, 0.35f);
        public static readonly Color HollowKnight= new Color(0.35f, 0.72f, 0.95f);
        public static readonly Color Wizard      = new Color(0.72f, 0.40f, 1.00f);
        public static readonly Color BabyDragon  = new Color(1.00f, 0.48f, 0.20f);
        public static readonly Color Gargoyle    = new Color(0.95f, 0.78f, 0.30f);
        public static readonly Color Lich        = new Color(0.85f, 0.95f, 1.00f);

        public static readonly Color PlayerBolt  = new Color(1.00f, 0.85f, 0.45f);
        public static readonly Color EmberShot   = new Color(1.00f, 0.55f, 0.18f);
        public static readonly Color FluxShot    = new Color(0.50f, 0.90f, 1.00f);
        public static readonly Color RuneShot    = new Color(0.80f, 0.45f, 1.00f);

        public static readonly Color HudInk      = new Color(0.88f, 0.92f, 1.00f);
        public static readonly Color HudDim      = new Color(0.52f, 0.57f, 0.68f);
        public static readonly Color HudGold     = new Color(1.00f, 0.82f, 0.35f);
        public static readonly Color HudBlood    = new Color(0.95f, 0.26f, 0.30f);
        public static readonly Color HudMint     = new Color(0.45f, 1.00f, 0.80f);

        public static Color Of(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Imp:          return Imp;
                case EnemyKind.HollowKnight: return HollowKnight;
                case EnemyKind.Wizard:       return Wizard;
                case EnemyKind.BabyDragon:   return BabyDragon;
                case EnemyKind.Gargoyle:     return Gargoyle;
                default:                     return Lich;
            }
        }
    }
}
