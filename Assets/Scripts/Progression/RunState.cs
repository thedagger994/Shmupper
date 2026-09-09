using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Everything that is true about the current run. Both halves of the central bargain live
    /// here: the derived player stats that upgrades buy, and the enemy multipliers those same
    /// upgrades pay for.
    public class RunState
    {
        public int Score;
        public int Essence;
        public int Floor = 1;
        public int Wave = 1;
        public int WavesThisFloor = 3;

        public int Combo;
        public float ComboTimer;
        public const float ComboWindow = 3.2f;

        public float FloorTime;
        public bool BoughtOnThisFloor;
        public bool TookDamageOnThisFloor;
        public int KillsTotal;

        public readonly Dictionary<UpgradeId, int> Levels = new Dictionary<UpgradeId, int>();

        public RunState()
        {
            foreach (var def in UpgradeCatalog.All) Levels[def.Id] = 0;
        }

        public int LevelOf(UpgradeId id) => Levels.TryGetValue(id, out int v) ? v : 0;

        /// The Keep learns from you: one threat point per upgrade tier bought, ever.
        public int ThreatLevel
        {
            get
            {
                int total = 0;
                foreach (var kv in Levels) total += kv.Value;
                return total;
            }
        }

        // ------------------------------------------------------------- player stats

        public float MaxHealth      => 100f + LevelOf(UpgradeId.Vitality) * 25f;
        public float DamageMul      => 1f + LevelOf(UpgradeId.SharpenedRunes) * 0.18f;
        public float FireRateMul    => 1f + LevelOf(UpgradeId.RapidSigils) * 0.12f;
        public float MoveSpeedMul   => 1f + LevelOf(UpgradeId.Quicksilver) * 0.09f;
        public float JumpMul        => 1f + LevelOf(UpgradeId.Quicksilver) * 0.05f;
        public int   FlaskCapacity  => 1 + LevelOf(UpgradeId.RuneFlask);
        public float AmmoMul        => 1f + LevelOf(UpgradeId.DeepReserves) * 0.40f;
        public float SiphonPerKill  => LevelOf(UpgradeId.Siphon) * 3f;
        public float DamageTakenMul => Mathf.Pow(0.89f, LevelOf(UpgradeId.Warding));

        // ------------------------------------------------------------- enemy scaling

        /// The Keep's answer to the player getting stronger.
        ///
        /// These terms add rather than multiply, and health is capped. Multiplying threat by
        /// depth compounds viciously - eight upgrades on floor four used to quadruple every
        /// health pool, which turned a fast arcade shooter into a chore of emptying magazines
        /// into things that would not fall over. Capping health at 2.6x keeps every enemy inside
        /// a handful of shots for the whole run.
        ///
        /// The pressure the design needs has been moved into the terms that do not slow the game
        /// down: enemies hit harder, and above all there are more of them. A crowded room is the
        /// genre's own answer to difficulty, and it makes the player shoot more rather than less.
        public float EnemyHealthMul => Mathf.Min(2.6f, 1f + ThreatLevel * 0.10f + (Floor - 1) * 0.12f);
        public float EnemyDamageMul => 1f + ThreatLevel * 0.09f + (Floor - 1) * 0.13f;
        public float EnemySpeedMul  => Mathf.Min(1.6f, 1f + ThreatLevel * 0.022f + (Floor - 1) * 0.03f);
        public int   EnemyCountBonus => ThreatLevel / 2 + (Floor - 1) * 2;

        // ------------------------------------------------------------- scoring

        public int ComboMultiplier
        {
            get
            {
                if (Combo >= 45) return 8;
                if (Combo >= 30) return 7;
                if (Combo >= 20) return 6;
                if (Combo >= 15) return 5;
                if (Combo >= 10) return 4;
                if (Combo >= 6) return 3;
                if (Combo >= 3) return 2;
                return 1;
            }
        }

        public void TickCombo(float dt)
        {
            if (Combo <= 0) return;

            ComboTimer -= dt;
            if (ComboTimer <= 0f) Combo = 0;
        }

        public int RegisterKill(int basePoints)
        {
            Combo++;
            ComboTimer = ComboWindow;
            KillsTotal++;

            int points = basePoints * ComboMultiplier;
            Score += points;
            Essence += Mathf.Max(1, basePoints / 8);
            return points;
        }

        public void AddScore(int points)
        {
            Score += Mathf.Max(0, points);
        }

        public bool CanAfford(UpgradeDef def) => Essence >= def.CostAt(LevelOf(def.Id));

        public bool IsMaxed(UpgradeDef def) => LevelOf(def.Id) >= def.MaxLevel;

        public bool Buy(UpgradeDef def)
        {
            if (def == null || IsMaxed(def) || !CanAfford(def)) return false;

            Essence -= def.CostAt(LevelOf(def.Id));
            Levels[def.Id] = LevelOf(def.Id) + 1;
            BoughtOnThisFloor = true;
            return true;
        }

        /// Par time is generous - the swift bonus should reward players who already know the
        /// weapons, not punish anyone who explores.
        public float ParTime => 55f + Floor * 18f;

        public int SwiftBonus
        {
            get
            {
                if (FloorTime > ParTime) return 0;
                float t = 1f - (FloorTime / ParTime);
                return Mathf.RoundToInt(3000f * t);
            }
        }

        public int NoShrineBonus => BoughtOnThisFloor ? 0 : 2000 * Floor;

        public int FlawlessBonus => TookDamageOnThisFloor ? 0 : 1500 * Floor;

        public void BeginFloor()
        {
            FloorTime = 0f;
            BoughtOnThisFloor = false;
            TookDamageOnThisFloor = false;
            Wave = 1;
            WavesThisFloor = Mathf.Clamp(2 + Floor / 2, 2, 5);
        }
    }
}
