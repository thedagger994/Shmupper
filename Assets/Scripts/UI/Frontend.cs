using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shmupper
{
    /// The cabinet front: attract loop, credit counter, high score table, initial entry and the
    /// end-of-run summary. It is written as one canvas with mutually exclusive pages because the
    /// arcade flow it imitates is strictly linear - there is nowhere to get lost.
    public class Frontend : MonoBehaviour
    {
        public event Action OnStartRequested;
        public event Action OnNameEntered;
        public event Action OnReturnToAttract;

        const float PageSeconds = 6.5f;

        Canvas _canvas;
        RunState _run;

        RectTransform _attractPage;
        RectTransform _gameOverPage;
        RectTransform _namePage;
        RectTransform _footer;

        TMP_Text _attractTitle;
        TMP_Text _attractHeading;
        TMP_Text _attractBody;
        TMP_Text _attractHint;

        TMP_Text _creditsText;
        TMP_Text _startText;
        TMP_Text _recordText;

        TMP_Text _gameOverBody;
        TMP_Text _gameOverTitle;

        TMP_Text _nameSlots;
        TMP_Text _namePrompt;

        int _credits;
        int _page;
        float _pageTimer;
        bool _attractActive;

        readonly char[] _initials = { 'A', 'A', 'A' };
        int _slot;
        int _pendingScore;
        int _pendingFloor;

        public int Credits => _credits;

        public void Init(Canvas canvas, RunState run)
        {
            _canvas = canvas;
            _run = run;
            Build();
            ShowAttract();
        }

        public void SetRun(RunState run) => _run = run;

        // -------------------------------------------------------------------- build

        void Build()
        {
            var root = UiKit.Node(_canvas.transform, "Frontend");
            UiKit.Stretch(root, "Vignette", new Color(0.02f, 0.02f, 0.04f, 0.55f));

            BuildAttract(root);
            BuildGameOver(root);
            BuildNameEntry(root);
            BuildFooter(root);
        }

        void BuildAttract(Transform root)
        {
            _attractPage = UiKit.Node(root, "AttractPage");

            _attractTitle = UiKit.Banner(_attractPage, "Title", "S H M U P P E R", 118, Palette.HudGold, 330f);
            UiKit.Banner(_attractPage, "Sub", "THE ARCANE KEEP", 34, Palette.HudMint, 250f);

            _attractHeading = UiKit.Banner(_attractPage, "Heading", "", 46, Palette.HudInk, 130f);
            _attractBody = UiKit.Label(_attractPage, "Body", "", 28, Palette.HudInk, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 80f), new Vector2(1500f, 500f));

            _attractHint = UiKit.Banner(_attractPage, "Hint", "", 24, Palette.HudDim, -330f);
        }

        void BuildGameOver(Transform root)
        {
            _gameOverPage = UiKit.Node(root, "GameOverPage");

            _gameOverTitle = UiKit.Banner(_gameOverPage, "Title", "THE KEEP KEEPS YOU", 76, Palette.HudBlood, 330f);
            _gameOverBody = UiKit.Label(_gameOverPage, "Body", "", 32, Palette.HudInk, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 210f), new Vector2(1400f, 520f));

            UiKit.Banner(_gameOverPage, "Hint", "PRESS ENTER", 26, Palette.HudDim, -330f);
            _gameOverPage.gameObject.SetActive(false);
        }

        void BuildNameEntry(Transform root)
        {
            _namePage = UiKit.Node(root, "NamePage");

            UiKit.Banner(_namePage, "Title", "A NEW NAME ON THE WALL", 62, Palette.HudGold, 300f);
            _namePrompt = UiKit.Banner(_namePage, "Prompt", "", 34, Palette.HudMint, 200f);

            _nameSlots = UiKit.Banner(_namePage, "Slots", "A A A", 150, Palette.HudInk, 0f);

            UiKit.Banner(_namePage, "Hint", "ARROWS TO CHOOSE      ENTER TO CARVE IT", 26, Palette.HudDim, -250f);
            _namePage.gameObject.SetActive(false);
        }

        void BuildFooter(Transform root)
        {
            _footer = UiKit.Node(root, "Footer");

            _recordText = UiKit.Label(_footer, "Record", "", 30, Palette.HudGold, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(1400f, 40f));

            _creditsText = UiKit.Label(_footer, "Credits", "", 34, Palette.HudInk, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 36f), new Vector2(800f, 44f));

            _startText = UiKit.Label(_footer, "Start", "", 32, Palette.HudMint, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 36f), new Vector2(900f, 44f));

            UiKit.Label(_footer, "Coin", "[C] INSERT COIN", 24, Palette.HudDim, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 8f), new Vector2(800f, 30f));
        }

        // -------------------------------------------------------------------- pages

        public void ShowAttract()
        {
            _attractActive = true;
            _page = 0;
            _pageTimer = 0f;

            _canvas.enabled = true;
            _attractPage.gameObject.SetActive(true);
            _gameOverPage.gameObject.SetActive(false);
            _namePage.gameObject.SetActive(false);
            _footer.gameObject.SetActive(true);

            RenderPage();
        }

        public void Hide()
        {
            _attractActive = false;
            _canvas.enabled = false;
        }

        public void ShowGameOver(RunState run, string summary, bool victorious)
        {
            _attractActive = false;
            _canvas.enabled = true;

            _attractPage.gameObject.SetActive(false);
            _namePage.gameObject.SetActive(false);
            _gameOverPage.gameObject.SetActive(true);
            _footer.gameObject.SetActive(true);

            _gameOverTitle.text = victorious ? "THE EMBER CORE IS YOURS" : "THE KEEP KEEPS YOU";
            _gameOverTitle.color = victorious ? Palette.HudGold : Palette.HudBlood;
            _gameOverBody.text = summary;
        }

        public void ShowNameEntry(int score, int floor)
        {
            _attractActive = false;
            _canvas.enabled = true;

            _pendingScore = score;
            _pendingFloor = floor;
            _initials[0] = _initials[1] = _initials[2] = 'A';
            _slot = 0;

            _attractPage.gameObject.SetActive(false);
            _gameOverPage.gameObject.SetActive(false);
            _namePage.gameObject.SetActive(true);
            _footer.gameObject.SetActive(false);

            _namePrompt.text = UiKit.Money(score) + "   -   FLOOR " + floor;
            RenderInitials();
        }

        void RenderInitials()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                bool active = i == _slot;
                sb.Append(active ? "<color=#FFD15A>" : "<color=#E0EAFF>");
                sb.Append(_initials[i]);
                sb.Append("</color>");
                if (i < 2) sb.Append("  ");
            }
            _nameSlots.text = sb.ToString();
        }

        // ------------------------------------------------------------------- update

        void Update()
        {
            UpdateFooter();

            if (_namePage.gameObject.activeSelf) { UpdateNameEntry(); return; }
            if (_gameOverPage.gameObject.activeSelf) { UpdateGameOver(); return; }
            if (_attractActive) UpdateAttract();
        }

        void UpdateFooter()
        {
            if (!_canvas.enabled) return;

            _recordText.text = "RECORD   " + UiKit.Money(SaveSystem.Record) + "   BY " + SaveSystem.RecordHolder;
            _creditsText.text = "CREDITS  " + _credits.ToString("00");

            bool canStart = _credits > 0;
            _startText.text = canStart ? "PRESS ENTER TO START" : "INSERT COIN TO PLAY";
            _startText.color = canStart
                ? Color.Lerp(Palette.HudMint, Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f))
                : Palette.HudDim;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.cKey.wasPressedThisFrame || kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
                InsertCredit();
        }

        public void InsertCredit()
        {
            _credits = Mathf.Min(99, _credits + 1);
            Sfx.Play2D(SfxId.Coin);
        }

        public bool ConsumeCredit()
        {
            if (_credits <= 0) return false;

            _credits--;
            return true;
        }

        void UpdateAttract()
        {
            _pageTimer += Time.unscaledDeltaTime;
            if (_pageTimer >= PageSeconds)
            {
                _pageTimer = 0f;
                _page = (_page + 1) % 6;
                RenderPage();
            }

            var kb = Keyboard.current;
            bool start = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                                        kb.spaceKey.wasPressedThisFrame);

            var pad = Gamepad.current;
            if (!start && pad != null) start = pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;

            if (!start) return;

            if (_credits <= 0)
            {
                Sfx.Play2D(SfxId.UiDeny);
                return;
            }

            Sfx.Play2D(SfxId.UiSelect);
            OnStartRequested?.Invoke();
        }

        void UpdateGameOver()
        {
            var kb = Keyboard.current;
            bool go = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                                     kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame);

            var pad = Gamepad.current;
            if (!go && pad != null) go = pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;

            if (!go) return;

            Sfx.Play2D(SfxId.UiSelect);
            OnReturnToAttract?.Invoke();
        }

        void UpdateNameEntry()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.leftArrowKey.wasPressedThisFrame) { _slot = (_slot + 2) % 3; Sfx.Play2D(SfxId.UiMove); RenderInitials(); }
            if (kb.rightArrowKey.wasPressedThisFrame) { _slot = (_slot + 1) % 3; Sfx.Play2D(SfxId.UiMove); RenderInitials(); }
            if (kb.upArrowKey.wasPressedThisFrame) { CycleLetter(1); }
            if (kb.downArrowKey.wasPressedThisFrame) { CycleLetter(-1); }

            // Typing the letters directly is faster than nudging a wheel, and every arcade
            // player eventually tries it.
            foreach (var key in kb.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;

                string named = key.displayName;
                if (string.IsNullOrEmpty(named) || named.Length != 1) continue;

                char c = char.ToUpperInvariant(named[0]);
                if ((c < 'A' || c > 'Z') && (c < '0' || c > '9')) continue;

                _initials[_slot] = c;
                _slot = Mathf.Min(2, _slot + 1);
                Sfx.Play2D(SfxId.UiMove);
                RenderInitials();
                break;
            }

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                SaveSystem.Submit(new string(_initials), _pendingScore, _pendingFloor, _run != null ? _run.ThreatLevel : 0);
                Sfx.Play2D(SfxId.UiSelect);
                OnNameEntered?.Invoke();
            }
        }

        void CycleLetter(int direction)
        {
            char c = _initials[_slot];
            int index = c >= 'A' && c <= 'Z' ? c - 'A' : 26 + (c - '0');
            index = (index + direction + 36) % 36;

            _initials[_slot] = index < 26 ? (char)('A' + index) : (char)('0' + (index - 26));
            Sfx.Play2D(SfxId.UiMove);
            RenderInitials();
        }

        // -------------------------------------------------------------- attract text

        void RenderPage()
        {
            bool titleVisible = _page == 0;
            _attractTitle.gameObject.SetActive(titleVisible);
            _attractPage.Find("Sub").gameObject.SetActive(titleVisible);

            switch (_page)
            {
                case 0:
                    _attractHeading.text = "";
                    _attractBody.text =
                        "THE KEEP REBUILDS ITSELF EVERY NIGHT FROM THE BONES OF THE LAST ONE.\n" +
                        "IT HAS SWALLOWED THE EMBER CORE, AND THE KINGDOM WITH IT.\n\n" +
                        "YOU ARE THE LAST GUNMAGE. GO DOWN. TAKE IT BACK.";
                    _attractHint.text = "A SHOOT EM UP IN A CASTLE THAT IS NEVER THE SAME TWICE";
                    break;

                case 1:
                    _attractHeading.text = "HOW TO PLAY";
                    _attractBody.text =
                        "W A S D   MOVE          MOUSE   LOOK          SPACE   JUMP\n" +
                        "LEFT MOUSE   FIRE       1 - 4 / WHEEL   SWITCH WEAPON\n" +
                        "F   DRINK A HEALING FLASK\n\n" +
                        "CLEAR EVERY WAVE ON THE FLOOR.\n" +
                        "FOLLOW THE COMPASS TO THE WAYSHRINE.\n" +
                        "SPEND ESSENCE, THEN DESCEND.\n\n" +
                        "NINE FLOORS DOWN LIES THE EMBER CORE. TAKE IT BACK.";
                    _attractHint.text = "STRAFE. NEVER STAND STILL.";
                    break;

                case 2:
                    _attractHeading.text = "THE BARGAIN";
                    _attractBody.text =
                        "EVERY UPGRADE YOU BUY AT A WAYSHRINE RAISES THE KEEP'S THREAT BY ONE.\n\n" +
                        "THREAT MAKES EVERY ENEMY TOUGHER, ANGRIER AND MORE NUMEROUS.\n" +
                        "THE KEEP LEARNS FROM YOU. IT ALWAYS HAS.\n\n" +
                        "WALK PAST THE SHRINE AND YOU STAY WEAK - BUT THE CASTLE STAYS KIND,\n" +
                        "AND IT PAYS A BONUS FOR THE INSULT.";
                    _attractHint.text = "HOW STRONG DARE YOU GET?";
                    break;

                case 3:
                    _attractHeading.text = "SCORING";
                    _attractBody.text =
                        "IMP . . . . . . . . . . . . . . .   100\n" +
                        "HOLLOW KNIGHT . . . . . . .   250\n" +
                        "WIZARD  . . . . . . . . . . . .   400\n" +
                        "BABY DRAGON . . . . . . . .   500\n" +
                        "GARGOYLE  . . . . . . . . . .   750\n" +
                        "THE PALE ARCHIVIST  . . . 5,000\n\n" +
                        "KILLS INSIDE THREE SECONDS CHAIN.\n" +
                        "3 CHAIN x2      6 x3      10 x4      15 x5      20 x6      30 x7      45 x8";
                    _attractHint.text = "THE CHAIN IS WHERE THE SCORE LIVES";
                    break;

                case 4:
                    _attractHeading.text = "BONUSES";
                    _attractBody.text =
                        "NO-SHRINE  . . . . . 2,000 x FLOOR    DESCEND WITHOUT BUYING ANYTHING\n" +
                        "FLAWLESS . . . . . . 1,500 x FLOOR    CLEAR A FLOOR UNTOUCHED\n" +
                        "SWIFT  . . . . . . . . UP TO 3,000        BEAT THE FLOOR'S PAR TIME\n" +
                        "DEPTH  . . . . . . . . 500 x FLOOR       AWARDED FOR EVERY WAVE CLEARED";
                    _attractHint.text = "GREED IS A STRATEGY. SO IS RESTRAINT.";
                    break;

                default:
                    _attractHeading.text = "TOP 10";
                    _attractBody.text = BuildScoreTable();
                    _attractHint.text = "CAN YOU GET ON THE WALL?";
                    break;
            }
        }

        public static string BuildScoreTable()
        {
            var sb = new StringBuilder();
            var entries = SaveSystem.Table.Entries;

            for (int i = 0; i < SaveSystem.TableSize; i++)
            {
                string rank = (i + 1).ToString("00");

                if (i >= entries.Count)
                {
                    sb.AppendLine(rank + "   ---           0        -");
                    continue;
                }

                var e = entries[i];
                string colour = i == 0 ? "#FFD15A" : "#E0EAFF";
                sb.AppendLine("<color=" + colour + ">" + rank + "   " + e.Name.PadRight(5) +
                              UiKit.Money(e.Score).PadLeft(10) + "     FLOOR " + e.Floor + "</color>");
            }

            return sb.ToString();
        }
    }
}
