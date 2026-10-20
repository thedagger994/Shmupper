using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shmupper
{
    /// The wayshrine screen. Keyboard only and deliberately terse: the player has just finished a
    /// floor and wants to spend and descend, not read. The one thing it must communicate loudly
    /// is the cost that is not measured in essence - every purchase raises the threat.
    ///
    /// The upgrade list is a real table: a bordered panel, a header, and each row built from
    /// separately positioned columns with its own background. It used to be one text label per
    /// row with the columns spaced by PadRight, which only lines up in a monospaced font and
    /// collapsed into ragged noise as soon as the interface moved to IM Fell English. Levels are
    /// drawn as pips - small filled rectangles - rather than as hash characters, so they read as
    /// a gauge at a glance.
    public class ShrineUI : MonoBehaviour
    {
        public event Action OnDescend;
        public event Action<UpgradeDef> OnPurchased;

        // Table geometry, in canvas units against the 1920x1080 reference.
        const float TableWidth = 1210f;
        const float ReportWidth = 430f;
        const float RowHeight = 62f;
        const float PanelGap = 22f;

        const float ColName = 26f;
        const float ColPips = 420f;
        const float ColCost = 680f;
        const float ColBlurb = 830f;

        static readonly Color PanelFill = new Color(0.035f, 0.045f, 0.075f, 0.95f);
        static readonly Color PanelEdge = new Color(0.45f, 1f, 0.85f, 0.30f);
        static readonly Color HeaderFill = new Color(0.45f, 1f, 0.85f, 0.10f);
        static readonly Color RowOdd = new Color(1f, 1f, 1f, 0.028f);
        static readonly Color RowEven = new Color(0f, 0f, 0f, 0f);
        static readonly Color RowPicked = new Color(0.45f, 1f, 0.85f, 0.17f);
        static readonly Color PipEmpty = new Color(1f, 1f, 1f, 0.13f);

        class Row
        {
            public Image Background;
            public Image Marker;
            public TMP_Text Name;
            public TMP_Text Cost;
            public TMP_Text Blurb;
            public Image[] Pips;
        }

        Canvas _canvas;
        RunState _run;
        RectTransform _page;

        TMP_Text _essenceText;
        TMP_Text _threatText;
        TMP_Text _reportText;
        TMP_Text _warningText;

        readonly List<Row> _rows = new List<Row>();
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
            UiKit.Stretch(_page, "Dim", new Color(0.015f, 0.02f, 0.035f, 0.90f));

            UiKit.Banner(_page, "Title", "W A Y S H R I N E", 72, Palette.ShrineGlow, 430f);
            UiKit.Banner(_page, "Sub", "THE KEEP IS WATCHING WHAT YOU TAKE", 25, Palette.HudDim, 372f);

            float totalWidth = TableWidth + PanelGap + ReportWidth;
            float leftX = -totalWidth * 0.5f;

            BuildTable(leftX);
            BuildReport(leftX + TableWidth + PanelGap);

            _warningText = UiKit.Banner(_page, "Warning", "", 26, Palette.HudBlood, -392f);
            UiKit.Banner(_page, "Hint", "UP / DOWN  CHOOSE      ENTER  BUY      ESC  DESCEND",
                24, Palette.HudDim, -442f);
        }

        void BuildTable(float leftX)
        {
            int count = UpgradeCatalog.All.Count;
            float bodyHeight = count * RowHeight;
            float panelHeight = bodyHeight + 128f;

            var panel = UiKit.Panel(_page, "TablePanel", PanelFill, PanelEdge,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
                new Vector2(leftX, 330f), new Vector2(TableWidth, panelHeight));

            // Totals strip across the top of the panel.
            _essenceText = Cell(panel, "Essence", 34, Palette.HudGold, TextAnchor.UpperLeft,
                new Vector2(ColName, -18f), 480f);

            _threatText = Cell(panel, "Threat", 24, Palette.HudBlood, TextAnchor.UpperRight,
                new Vector2(TableWidth - 26f - 620f, -24f), 620f);

            // Header row.
            UiKit.Rect(panel, "HeaderBg", HeaderFill, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -68f), new Vector2(TableWidth - 4f, 36f));

            Cell(panel, "HName", 21, Palette.ShrineGlow, TextAnchor.UpperLeft, new Vector2(ColName, -74f), 380f)
                .text = "UPGRADE";
            Cell(panel, "HPips", 21, Palette.ShrineGlow, TextAnchor.UpperLeft, new Vector2(ColPips, -74f), 240f)
                .text = "RANK";
            Cell(panel, "HCost", 21, Palette.ShrineGlow, TextAnchor.UpperRight, new Vector2(ColCost - 120f, -74f), 120f)
                .text = "COST";
            Cell(panel, "HBlurb", 21, Palette.ShrineGlow, TextAnchor.UpperLeft, new Vector2(ColBlurb, -74f), 360f)
                .text = "EFFECT";

            for (int i = 0; i < count; i++)
                _rows.Add(BuildRow(panel, UpgradeCatalog.All[i], i, -104f - i * RowHeight));
        }

        Row BuildRow(RectTransform panel, UpgradeDef def, int index, float y)
        {
            var row = new Row();

            row.Background = UiKit.Rect(panel, "Row" + index, index % 2 == 0 ? RowOdd : RowEven,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, y + 4f), new Vector2(TableWidth - 4f, RowHeight - 4f));

            // A bright bar down the left edge of the selected row. Clearer than a cursor glyph
            // and it survives the row being dimmed when unaffordable.
            row.Marker = UiKit.Rect(panel, "Marker" + index, Color.clear,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(6f, y + 4f), new Vector2(5f, RowHeight - 4f));

            row.Name = Cell(panel, "Name" + index, 25, Palette.HudInk, TextAnchor.UpperLeft,
                new Vector2(ColName, y - 4f), 380f);
            row.Name.text = def.Name;

            row.Pips = new Image[def.MaxLevel];
            for (int p = 0; p < def.MaxLevel; p++)
            {
                row.Pips[p] = UiKit.Rect(panel, "Pip" + index + "_" + p, PipEmpty,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(ColPips + p * 30f, y - 16f), new Vector2(21f, 21f));
            }

            row.Cost = Cell(panel, "Cost" + index, 25, Palette.HudGold, TextAnchor.UpperRight,
                new Vector2(ColCost - 120f, y - 4f), 120f);

            row.Blurb = Cell(panel, "Blurb" + index, 20, Palette.HudDim, TextAnchor.UpperLeft,
                new Vector2(ColBlurb, y - 2f), 360f);
            row.Blurb.text = def.Blurb;

            return row;
        }

        void BuildReport(float leftX)
        {
            int count = UpgradeCatalog.All.Count;
            float panelHeight = count * RowHeight + 128f;

            var panel = UiKit.Panel(_page, "ReportPanel", PanelFill, PanelEdge,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
                new Vector2(leftX, 330f), new Vector2(ReportWidth, panelHeight));

            UiKit.Rect(panel, "ReportHeaderBg", HeaderFill, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -18f), new Vector2(ReportWidth - 4f, 36f));

            Cell(panel, "ReportHeader", 21, Palette.ShrineGlow, TextAnchor.UpperLeft,
                new Vector2(24f, -24f), 360f).text = "FLOOR REPORT";

            _reportText = Cell(panel, "Report", 23, Palette.HudMint, TextAnchor.UpperLeft,
                new Vector2(24f, -70f), ReportWidth - 48f);

            // The report is the one place with wrapped multi-line text, so it keeps word wrapping
            // and gets a generous height.
            _reportText.textWrappingMode = TextWrappingModes.Normal;
            _reportText.rectTransform.sizeDelta = new Vector2(ReportWidth - 48f, panelHeight - 90f);
        }

        /// A single text cell anchored to the top-left of its panel.
        TMP_Text Cell(RectTransform panel, string name, int size, Color color, TextAnchor anchor,
                      Vector2 position, float width)
        {
            return UiKit.Label(panel, name, "", size, color, anchor,
                new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(width, size + 16f));
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
            _essenceText.text = "ESSENCE  " + UiKit.Money(_run.Essence);
            _threatText.text = "KEEP THREAT  " + _run.ThreatLevel +
                               "     ENEMY HEALTH x" + _run.EnemyHealthMul.ToString("0.00");

            for (int i = 0; i < _rows.Count; i++)
            {
                var def = UpgradeCatalog.All[i];
                var row = _rows[i];

                int level = _run.LevelOf(def.Id);
                bool selected = i == _cursor;
                bool maxed = _run.IsMaxed(def);
                bool affordable = _run.CanAfford(def);

                row.Background.color = selected ? RowPicked : (i % 2 == 0 ? RowOdd : RowEven);
                row.Marker.color = selected ? Palette.ShrineGlow : Color.clear;

                // Name carries the upgrade's own colour so the table is scannable; it desaturates
                // to grey when maxed and reds out when the player cannot afford the next rank.
                row.Name.color = maxed
                    ? Palette.HudDim
                    : selected
                        ? (affordable ? Color.white : Palette.HudBlood)
                        : (affordable ? def.Tint : Palette.HudDim);

                for (int p = 0; p < row.Pips.Length; p++)
                    row.Pips[p].color = p < level ? def.Tint : PipEmpty;

                row.Cost.text = maxed ? "MAX" : UiKit.Money(def.CostAt(level));
                row.Cost.color = maxed
                    ? Palette.HudDim
                    : affordable ? Palette.HudGold : Palette.HudBlood;

                row.Blurb.color = selected ? Palette.HudInk : Palette.HudDim;
            }

            _reportText.text = _report;

            _warningText.text = _run.BoughtOnThisFloor
                ? "THE KEEP HAS TAKEN YOUR MEASURE - NO-SHRINE BONUS FORFEIT"
                : "DESCEND WITHOUT BUYING FOR " + UiKit.Money(_run.NoShrineBonus) + " BONUS";
            _warningText.color = _run.BoughtOnThisFloor ? Palette.HudBlood : Palette.HudMint;
        }

        /// Composed by the game manager after a floor is cleared and handed straight to the
        /// shrine, so the player reads their payout at the moment they decide how to spend it.
        ///
        /// Columns are set with TextMeshPro's pos tag rather than padded with spaces, which does
        /// not align in a proportional face.
        public static string ComposeReport(RunState run, int waveBonus, int swift, int flawless, int total)
        {
            const string col = "<pos=58%>";

            var sb = new StringBuilder();
            sb.AppendLine("<color=#FFFFFF>FLOOR " + run.Floor + " CLEARED</color>");
            sb.AppendLine("");
            sb.AppendLine("TIME" + col + run.FloorTime.ToString("0.0") + "S");
            sb.AppendLine("PAR" + col + run.ParTime.ToString("0") + "S");
            sb.AppendLine("KILLS" + col + run.KillsTotal);
            sb.AppendLine("");
            sb.AppendLine("DEPTH" + col + UiKit.Money(waveBonus));
            sb.AppendLine("SWIFT" + col + UiKit.Money(swift));
            sb.AppendLine("FLAWLESS" + col + UiKit.Money(flawless));
            sb.AppendLine("");
            sb.AppendLine("<color=#FFFFFF>TOTAL" + col + UiKit.Money(total) + "</color>");
            return sb.ToString();
        }
    }
}
