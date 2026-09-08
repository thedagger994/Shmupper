using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shmupper
{
    /// The wayshrine screen. Keyboard only and deliberately terse: the player has just finished a
    /// floor and wants to spend and descend, not read. The one thing it must communicate loudly
    /// is the cost that is not measured in essence - every purchase raises the threat.
    public class ShrineUI : MonoBehaviour
    {
        public event Action OnDescend;
        public event Action<UpgradeDef> OnPurchased;

        Canvas _canvas;
        RunState _run;
        RectTransform _page;

        Text _essenceText;
        Text _threatText;
        Text _reportText;
        Text _warningText;

        readonly List<Text> _rows = new List<Text>();
        int _cursor;
        string _report = "";

        public bool IsOpen => _canvas != null && _canvas.enabled;

        public void SetRun(RunState run) => _run = run;

        public void Init(Canvas canvas, RunState run)
        {
            _canvas = canvas;
            _run = run;
            Build();
            _canvas.enabled = false;
        }

        void Build()
        {
            _page = UiKit.Node(_canvas.transform, "ShrinePage");
            UiKit.Stretch(_page, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.86f));

            UiKit.Banner(_page, "Title", "W A Y S H R I N E", 76, Palette.ShrineGlow, 400f);
            UiKit.Banner(_page, "Sub", "THE KEEP IS WATCHING WHAT YOU TAKE", 26, Palette.HudDim, 340f);

            _essenceText = UiKit.Label(_page, "Essence", "", 40, Palette.HudGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -220f), new Vector2(700f, 50f));

            _threatText = UiKit.Label(_page, "Threat", "", 30, Palette.HudBlood, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -270f), new Vector2(700f, 40f));

            for (int i = 0; i < UpgradeCatalog.All.Count; i++)
            {
                var row = UiKit.Label(_page, "Row" + i, "", 28, Palette.HudInk, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(120f, -340f - i * 52f), new Vector2(1100f, 44f));
                _rows.Add(row);
            }

            _reportText = UiKit.Label(_page, "Report", "", 26, Palette.HudMint, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -220f), new Vector2(700f, 460f));

            _warningText = UiKit.Banner(_page, "Warning", "", 26, Palette.HudBlood, -330f);
            UiKit.Banner(_page, "Hint", "UP / DOWN CHOOSE      ENTER BUY      ESC DESCEND", 26, Palette.HudDim, -390f);
        }

        public void Open(string floorReport)
        {
            _report = floorReport;
            _cursor = 0;
            _canvas.enabled = true;
            Sfx.Play2D(SfxId.Shrine);
            Refresh();
        }

        public void Close()
        {
            _canvas.enabled = false;
        }

        void Update()
        {
            if (!IsOpen) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                Buy();

            if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            {
                Sfx.Play2D(SfxId.Descend);
                OnDescend?.Invoke();
            }
        }

        void Move(int direction)
        {
            _cursor = (_cursor + direction + UpgradeCatalog.All.Count) % UpgradeCatalog.All.Count;
            Sfx.Play2D(SfxId.UiMove);
            Refresh();
        }

        void Buy()
        {
            var def = UpgradeCatalog.All[_cursor];

            if (!_run.Buy(def))
            {
                Sfx.Play2D(SfxId.UiDeny);
                return;
            }

            Sfx.Play2D(SfxId.UiSelect);
            OnPurchased?.Invoke(def);
            Refresh();
        }

        void Refresh()
        {
            _essenceText.text = "ESSENCE   " + UiKit.Money(_run.Essence);
            _threatText.text = "KEEP THREAT   " + _run.ThreatLevel +
                               "      ENEMY HEALTH x" + _run.EnemyHealthMul.ToString("0.00");

            for (int i = 0; i < _rows.Count; i++)
            {
                var def = UpgradeCatalog.All[i];
                int level = _run.LevelOf(def.Id);
                bool selected = i == _cursor;
                bool maxed = _run.IsMaxed(def);
                bool affordable = _run.CanAfford(def);

                string pips = "";
                for (int p = 0; p < def.MaxLevel; p++) pips += p < level ? "#" : ".";

                string cost = maxed ? "MAX" : UiKit.Money(def.CostAt(level));
                string line = (selected ? "> " : "  ") + def.Name.PadRight(20) + pips.PadRight(9) +
                              cost.PadLeft(6) + "   " + def.Blurb;

                _rows[i].text = line;
                _rows[i].color = maxed
                    ? Palette.HudDim
                    : selected
                        ? (affordable ? Color.white : Palette.HudBlood)
                        : (affordable ? def.Tint : Palette.HudDim);
            }

            _reportText.text = _report;

            _warningText.text = _run.BoughtOnThisFloor
                ? "THE KEEP HAS TAKEN YOUR MEASURE - NO-SHRINE BONUS FORFEIT"
                : "DESCEND WITHOUT BUYING FOR " + UiKit.Money(_run.NoShrineBonus) + " BONUS";
            _warningText.color = _run.BoughtOnThisFloor ? Palette.HudBlood : Palette.HudMint;
        }

        /// Composed by the game manager after a floor is cleared and handed straight to the
        /// shrine, so the player reads their payout at the moment they decide how to spend it.
        public static string ComposeReport(RunState run, int waveBonus, int swift, int flawless, int total)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FLOOR " + run.Floor + " CLEARED");
            sb.AppendLine("");
            sb.AppendLine("TIME        " + run.FloorTime.ToString("0.0") + "S   (PAR " + run.ParTime.ToString("0") + "S)");
            sb.AppendLine("KILLS SO FAR   " + run.KillsTotal);
            sb.AppendLine("");
            sb.AppendLine("DEPTH BONUS     " + UiKit.Money(waveBonus));
            sb.AppendLine("SWIFT BONUS     " + UiKit.Money(swift));
            sb.AppendLine("FLAWLESS BONUS  " + UiKit.Money(flawless));
            sb.AppendLine("");
            sb.AppendLine("FLOOR TOTAL     " + UiKit.Money(total));
            return sb.ToString();
        }
    }
}
