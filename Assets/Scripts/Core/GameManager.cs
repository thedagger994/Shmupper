using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmupper
{
    /// The spine of the game. It owns the phase machine, builds and tears down floors, wires the
    /// systems to each other, and turns the wave director's events into score, bonuses and screen
    /// transitions. Everything else in the project is deliberately ignorant of everything else -
    /// this is the only class that knows the whole shape of a run.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.Attract;

        RunState _run;
        PlayerRig _rig;
        WaveDirector _director;
        AttractStage _attract;

        Hud _hud;
        Frontend _frontend;
        ShrineUI _shrine;

        DungeonMap _map;
        FlowField _flow;
        LevelBuilder.BuiltLevel _level;
        Wayshrine _wayshrine;

        AudioListener _listener;
        Transform _listenerTransform;

        float _flowTimer;
        int _seed;

        void Awake()
        {
            Instance = this;

            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 1;

            Sfx.Init();
            Layers.ConfigureMatrix();
            SaveSystem.Load();
            ConfigureRendering();

            _run = new RunState();

            BuildListener();
            BuildInterface();

            _rig = PlayerRig.Create(_run);
            _rig.Health.OnDied += HandlePlayerDied;

            _director = gameObject.AddComponent<WaveDirector>();
            _director.Init(_run, _rig.Root.transform, _rig.Health, _rig.Controller, _rig.Weapons);
            _director.OnWaveCleared += HandleWaveCleared;
            _director.OnFloorCleared += HandleFloorCleared;

            _hud.Init(_hudCanvas, _rig.Controller, _rig.Health, _rig.Weapons, _run, _director);

            _attract = AttractStage.Create();

            EnterAttract();
        }

        Canvas _hudCanvas;

        /// A dark, foggy castle needs its atmosphere set once, globally. URP reads these same
        /// RenderSettings values, so no volume asset is needed.
        static void ConfigureRendering()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.16f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.fogDensity = 0.019f;
        }

        void BuildListener()
        {
            var go = new GameObject("Listener");
            _listener = go.AddComponent<AudioListener>();
            _listenerTransform = go.transform;
        }

        void BuildInterface()
        {
            _hudCanvas = UiKit.CreateCanvas("HudCanvas", UiKit.SortHud);
            _hud = _hudCanvas.gameObject.AddComponent<Hud>();

            var frontCanvas = UiKit.CreateCanvas("FrontendCanvas", UiKit.SortFrontend);
            _frontend = frontCanvas.gameObject.AddComponent<Frontend>();
            _frontend.Init(frontCanvas, _run);
            _frontend.OnStartRequested += StartRun;
            _frontend.OnReturnToAttract += EnterAttract;
            _frontend.OnNameEntered += HandleNameEntered;

            var shrineCanvas = UiKit.CreateCanvas("ShrineCanvas", UiKit.SortFrontend - 5);
            _shrine = shrineCanvas.gameObject.AddComponent<ShrineUI>();
            _shrine.Init(shrineCanvas, _run);
            _shrine.OnDescend += Descend;
            _shrine.OnPurchased += HandlePurchase;
        }

        // -------------------------------------------------------------------- phases

        void EnterAttract()
        {
            Phase = GamePhase.Attract;
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            TearDownFloor();
            _director.ClearAll();

            _rig.SetActive(false);
            _attract.SetActive(true);

            _hud.SetVisible(false);
            _shrine.Close();
            _frontend.ShowAttract();
        }

        void StartRun()
        {
            if (!_frontend.ConsumeCredit()) return;

            _run = new RunState();
            _victorious = false;
            RebindRun();

            Phase = GamePhase.Playing;
            Time.timeScale = 1f;

            _attract.SetActive(false);
            _rig.SetActive(true);
            _rig.SetInputEnabled(true);
            _rig.Health.FullRestore();
            _rig.Weapons.RefillAll();

            _frontend.Hide();
            _hud.SetVisible(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _seed = Random.Range(1, int.MaxValue);
            BuildFloor(1);
        }

        /// A new run means a new RunState instance, so every system holding a reference has to be
        /// pointed at it again. The systems themselves are never rebuilt - recreating them would
        /// leave the old objects' event subscriptions alive and firing into destroyed components.
        void RebindRun()
        {
            _rig.Controller.SetRun(_run);
            _rig.Health.Init(_run, _rig.Controller);
            _rig.Weapons.ResetForNewRun(_run);

            _director.SetRun(_run);
            _frontend.SetRun(_run);
            _shrine.SetRun(_run);
            _hud.SetRun(_run);
        }

        void BuildFloor(int floor)
        {
            TearDownFloor();

            _run.Floor = floor;
            _run.BeginFloor();

            _map = DungeonGenerator.Generate(_seed + floor * 7919, floor);
            _level = LevelBuilder.Build(_map, floor);
            _flow = new FlowField(_map);

            _director.SetFloor(_map, _flow);

            _rig.Controller.Teleport(_level.PlayerSpawn, Random.Range(0f, 360f));
            _rig.Weapons.UnlockForFloor(floor);
            _rig.Health.RefillForFloor();

            _wayshrine = Wayshrine.Create(_level.ShrinePosition, _rig.Root.transform);
            _wayshrine.OnPlayerArrived += OpenShrine;

            _flow.Rebuild(_rig.Root.transform.position);

            _hud.SetObjective(null, "");
            _hud.Announce("FLOOR " + floor + "   -   " + FloorName(floor));

            StartCoroutine(BeginWaveAfter(1.6f, 1));
        }

        static string FloorName(int floor)
        {
            switch ((floor - 1) % 5)
            {
                case 0: return "THE OUTER WARD";
                case 1: return "THE GREEN CLOISTER";
                case 2: return "THE EMBER KILNS";
                case 3: return "THE DROWNED ARCHIVE";
                default: return "THE THRONE OF ASH";
            }
        }

        IEnumerator BeginWaveAfter(float delay, int wave)
        {
            yield return new WaitForSeconds(delay);

            if (Phase != GamePhase.Playing) yield break;

            _run.Wave = wave;
            _director.StartWave(wave);
        }

        void TearDownFloor()
        {
            if (_level != null && _level.Root != null) Destroy(_level.Root);
            if (_wayshrine != null) Destroy(_wayshrine.gameObject);

            _level = null;
            _wayshrine = null;
            _map = null;
            _flow = null;
        }

        // -------------------------------------------------------------------- events

        void HandleWaveCleared(int wave)
        {
            if (Phase != GamePhase.Playing) return;

            int bonus = 500 * _run.Floor;
            _run.AddScore(bonus);

            if (wave < _run.WavesThisFloor)
            {
                _hud.Announce("WAVE CLEARED   +" + UiKit.Money(bonus));
                StartCoroutine(BeginWaveAfter(3.4f, wave + 1));
            }
        }

        /// The Keep is nine floors deep. Clearing the ninth - which is a boss floor, since every
        /// third one is - is the win condition the story promises, and it has to actually exist
        /// or the objective is a lie.
        public const int FinalFloor = 9;

        void HandleFloorCleared()
        {
            if (Phase != GamePhase.Playing) return;

            Phase = GamePhase.Intermission;

            int waveBonus = 500 * _run.Floor * _run.WavesThisFloor;
            int swift = _run.SwiftBonus;
            int flawless = _run.FlawlessBonus;

            _run.AddScore(swift + flawless);
            _run.Essence += (swift + flawless) / 12;

            _pendingReport = ShrineUI.ComposeReport(_run, waveBonus, swift, flawless, waveBonus + swift + flawless);

            if (_run.Floor >= FinalFloor)
            {
                ClaimTheCore();
                return;
            }

            _wayshrine.SetArmed(true);
            _hud.SetObjective(_wayshrine.transform, "WAYSHRINE");
            _hud.Announce("FLOOR CLEARED   -   FIND THE WAYSHRINE");

            Sfx.Play2D(SfxId.Shrine);
        }

        string _pendingReport = "";

        void OpenShrine()
        {
            if (Phase != GamePhase.Intermission) return;

            Phase = GamePhase.Shrine;

            _rig.SetInputEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;
            _shrine.Open(_pendingReport);
        }

        void HandlePurchase(UpgradeDef def)
        {
            switch (def.Id)
            {
                case UpgradeId.Vitality:      _rig.Health.OnMaxHealthRaised(); break;
                case UpgradeId.RuneFlask:     _rig.Health.OnFlaskCapacityRaised(); break;
                case UpgradeId.DeepReserves:  _rig.Weapons.RefillAll(); break;
            }
        }

        void Descend()
        {
            if (Phase != GamePhase.Shrine && Phase != GamePhase.Intermission) return;

            int noShrine = _run.NoShrineBonus;
            if (noShrine > 0)
            {
                _run.AddScore(noShrine);
                _hud.Announce("NO-SHRINE BONUS   +" + UiKit.Money(noShrine));
            }

            _shrine.Close();
            Time.timeScale = 1f;

            Phase = GamePhase.Playing;
            _rig.SetInputEnabled(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Sfx.Play2D(SfxId.Descend);
            BuildFloor(_run.Floor + 1);
        }

        /// Taking the Ember Core pays out everything the run was still holding: the depths cleared
        /// and, generously, the essence never spent - the last reward for refusing the bargain.
        void ClaimTheCore()
        {
            _victorious = true;

            int coreBonus = 25000 + _run.Essence * 4;
            _run.AddScore(coreBonus);

            Fx.Explosion(_rig.Root.transform.position + Vector3.up * 2f, 12f, Palette.HudGold);
            _rig.Controller.Shake(0.8f, 1.2f);
            Sfx.Play2D(SfxId.Shrine);

            _hud.Announce("THE EMBER CORE IS YOURS   +" + UiKit.Money(coreBonus));
            StartCoroutine(EndRunAfter(3.2f));
        }

        IEnumerator EndRunAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndRun();
        }

        void HandlePlayerDied()
        {
            _victorious = false;
            EndRun();
        }

        bool _victorious;

        void EndRun()
        {
            if (Phase == GamePhase.GameOver || Phase == GamePhase.NameEntry) return;

            Phase = GamePhase.GameOver;
            Time.timeScale = 1f;

            _rig.SetInputEnabled(false);
            _director.ClearAll();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _hud.SetVisible(false);
            Sfx.Play2D(_victorious ? SfxId.Shrine : SfxId.EnemyDie, 0.6f);

            if (SaveSystem.Qualifies(_run.Score))
            {
                Phase = GamePhase.NameEntry;
                _frontend.ShowNameEntry(_run.Score, _run.Floor);
            }
            else
            {
                _frontend.ShowGameOver(_run, BuildSummary(), _victorious);
            }
        }

        void HandleNameEntered()
        {
            Phase = GamePhase.GameOver;
            _frontend.ShowGameOver(_run, BuildSummary(), _victorious);
        }

        string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("FINAL SCORE      " + UiKit.Money(_run.Score));
            sb.AppendLine("");
            sb.AppendLine(_victorious
                ? "THE KEEP FELL     ALL " + FinalFloor + " FLOORS"
                : "REACHED          FLOOR " + _run.Floor + "  -  " + FloorName(_run.Floor));
            sb.AppendLine("SOULS UNMADE     " + _run.KillsTotal);
            sb.AppendLine("KEEP THREAT      " + _run.ThreatLevel);
            sb.AppendLine("ESSENCE LEFT     " + UiKit.Money(_run.Essence));
            sb.AppendLine("");
            sb.AppendLine(_run.Score >= SaveSystem.Record ? "A NEW RECORD STANDS ON THE WALL" : "THE RECORD IS " + UiKit.Money(SaveSystem.Record));
            return sb.ToString();
        }

        // -------------------------------------------------------------------- update

        void Update()
        {
            FollowListener();
            HandleGlobalKeys();

            if (Phase != GamePhase.Playing && Phase != GamePhase.Intermission) return;

            float dt = Time.deltaTime;
            _run.TickCombo(dt);
            if (Phase == GamePhase.Playing) _run.FloorTime += dt;

            _flowTimer -= dt;
            if (_flowTimer <= 0f && _flow != null)
            {
                _flowTimer = 0.22f;
                _flow.Rebuild(_rig.Root.transform.position);
            }
        }

        /// One listener follows whichever camera is live, so audio works identically in the
        /// attract diorama and in the castle without ever having two listeners in the scene.
        void FollowListener()
        {
            Transform target = Phase == GamePhase.Attract && _attract != null && _attract.StageCamera != null
                ? _attract.StageCamera.transform
                : _rig?.Camera?.transform;

            if (target == null || _listenerTransform == null) return;

            _listenerTransform.SetPositionAndRotation(target.position, target.rotation);
        }

        void HandleGlobalKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame && Phase == GamePhase.Playing)
            {
                // Escape during play releases the mouse rather than opening a menu: this is an
                // arcade cabinet, and there is nothing to pause into.
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
                _rig.Controller.LookEnabled = Cursor.lockState == CursorLockMode.Locked;
            }

            if (Phase == GamePhase.Playing && Cursor.lockState != CursorLockMode.Locked &&
                Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _rig.Controller.LookEnabled = true;
            }
        }
    }
}
