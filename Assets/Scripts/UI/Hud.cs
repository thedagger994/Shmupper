using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shmupper
{
    /// The in-game display. The brief asked for a compass with weapon and health information,
    /// the flask, and a crosshair, so those four sit at the corners of the screen and everything
    /// else - score, depth, threat - is arranged around them without crowding the centre.
    ///
    /// The compass does the heavy lifting: it is the only thing telling the player where the
    /// remaining enemies and the shrine are in a castle they have never seen before.
    public class Hud : MonoBehaviour
    {
        const float CompassWidth = 760f;
        const float CompassHalfFov = 90f;
        const int MaxEnemyMarkers = 10;

        Canvas _canvas;
        PlayerController _player;
        PlayerHealth _health;
        WeaponSystem _weapons;
        RunState _run;
        WaveDirector _director;
        Transform _objective;
        string _objectiveLabel = "";

        RectTransform _compassStrip;
        readonly List<Image> _enemyMarkers = new List<Image>();
        readonly List<TMP_Text> _cardinalLabels = new List<TMP_Text>();
        Image _objectiveMarker;

        static readonly string[] CardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        Image _healthFill;
        TMP_Text _healthText, _healthShadow;
        TMP_Text _weaponText, _weaponShadow;
        TMP_Text _ammoText, _ammoShadow;
        TMP_Text _weaponListText;
        TMP_Text _scoreText, _scoreShadow;
        TMP_Text _recordText;
        TMP_Text _floorText, _floorShadow;
        TMP_Text _threatText;
        TMP_Text _comboText;
        TMP_Text _announceText;
        TMP_Text _objectiveText;
        TMP_Text _hintText;

        readonly List<Image> _flaskIcons = new List<Image>();
        RectTransform _flaskRow;

        Image _damageFlash;
        Image[] _crosshair = new Image[4];
        Image _crosshairDot;

        RectTransform _bossBar;
        Image _bossFill;
        TMP_Text _bossLabel;

        float _announceTimer;
        float _flashAmount;
        float _crosshairKick;
        float _comboFlash;

        public void Init(Canvas canvas, PlayerController player, PlayerHealth health,
                         WeaponSystem weapons, RunState run, WaveDirector director)
        {
            _canvas = canvas;
            _player = player;
            _health = health;
            _weapons = weapons;
            _run = run;
            _director = director;

            Build();

            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnFlasksChanged += HandleFlasksChanged;
            _health.OnHurt += _ => _flashAmount = 1f;

            _weapons.OnWeaponChanged += HandleWeaponChanged;
            _weapons.OnAnnounce += Announce;

            _director.OnWaveStarted += (w, total) => Announce("WAVE " + w + " OF " + total);
            _director.OnEnemyKilled += (_, points) => { _comboFlash = 1f; };

            HandleHealthChanged(_health.Health, _health.MaxHealth);
            HandleFlasksChanged(_health.Flasks, _health.FlaskCapacity);
            HandleWeaponChanged(_weapons.Current);
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnFlasksChanged -= HandleFlasksChanged;
            }

            if (_weapons != null)
            {
                _weapons.OnWeaponChanged -= HandleWeaponChanged;
                _weapons.OnAnnounce -= Announce;
            }
        }

        public void SetRun(RunState run) => _run = run;

        public void SetObjective(Transform target, string label)
        {
            _objective = target;
            _objectiveLabel = label;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.enabled = visible;
        }

        public void Announce(string message)
        {
            if (_announceText == null) return;

            _announceText.text = message;
            _announceTimer = 2.6f;
        }

        // -------------------------------------------------------------------- build

        void Build()
        {
            var root = UiKit.Node(_canvas.transform, "Hud");

            BuildDamageFlash(root);
            BuildCrosshair(root);
            BuildCompass(root);
            BuildTopLeft(root);
            BuildTopRight(root);
            BuildBottomLeft(root);
            BuildBottomRight(root);
            BuildCentre(root);
            BuildBossBar(root);
        }

        void BuildDamageFlash(Transform root)
        {
            _damageFlash = UiKit.Stretch(root, "DamageFlash", new Color(0.75f, 0.05f, 0.08f, 0f));
        }

        void BuildCrosshair(Transform root)
        {
            var centre = UiKit.Node(root, "Crosshair");

            for (int i = 0; i < 4; i++)
            {
                bool vertical = i < 2;
                _crosshair[i] = UiKit.Rect(centre, "Tick" + i, new Color(1f, 1f, 1f, 0.85f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    vertical ? new Vector2(2.5f, 11f) : new Vector2(11f, 2.5f));
            }

            _crosshairDot = UiKit.Rect(centre, "Dot", new Color(1f, 0.9f, 0.5f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2.5f, 2.5f));
        }

        void BuildCompass(Transform root)
        {
            var frame = UiKit.Rect(root, "CompassFrame", new Color(0.04f, 0.05f, 0.08f, 0.55f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f),
                new Vector2(CompassWidth + 8f, 52f));

            var strip = UiKit.Rect(frame.transform, "CompassStrip", new Color(0f, 0f, 0f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CompassWidth, 46f));
            strip.gameObject.AddComponent<RectMask2D>();
            _compassStrip = strip.rectTransform;

            for (int i = 0; i < CardinalNames.Length; i++)
            {
                var label = UiKit.Label(_compassStrip, "Cardinal" + i, CardinalNames[i],
                    CardinalNames[i].Length == 1 ? 22 : 16,
                    CardinalNames[i].Length == 1 ? Palette.HudInk : Palette.HudDim,
                    TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(60f, 28f));
                _cardinalLabels.Add(label);
            }

            _objectiveMarker = UiKit.Rect(_compassStrip, "Objective", Palette.ShrineGlow,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 30f));

            for (int i = 0; i < MaxEnemyMarkers; i++)
            {
                var marker = UiKit.Rect(_compassStrip, "Enemy" + i, Palette.HudBlood,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 16f));
                marker.enabled = false;
                _enemyMarkers.Add(marker);
            }

            // A fixed centre notch so the player can read the compass without moving the mouse.
            UiKit.Rect(frame.transform, "Needle", Palette.HudGold,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(3f, 12f));

            _objectiveText = UiKit.Label(root, "ObjectiveText", "", 22, Palette.HudMint, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(900f, 30f));
        }

        void BuildTopLeft(Transform root)
        {
            _floorText = UiKit.Shadowed(root, "Floor", "", 30, Palette.HudInk, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -26f), new Vector2(600f, 40f), out _floorShadow);

            _threatText = UiKit.Label(root, "Threat", "", 22, Palette.HudBlood, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -62f), new Vector2(600f, 30f));
        }

        void BuildTopRight(Transform root)
        {
            _scoreText = UiKit.Shadowed(root, "Score", "0", 40, Palette.HudGold, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -22f), new Vector2(700f, 50f), out _scoreShadow);

            _recordText = UiKit.Label(root, "Record", "", 21, Palette.HudDim, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -66f), new Vector2(700f, 30f));
        }

        void BuildBottomLeft(Transform root)
        {
            UiKit.Rect(root, "HealthBack", new Color(0.05f, 0.05f, 0.08f, 0.72f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 44f), new Vector2(430f, 34f));

            _healthFill = UiKit.Rect(root, "HealthFill", Palette.HudBlood,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 46f), new Vector2(426f, 30f));
            _healthFill.rectTransform.pivot = new Vector2(0f, 0f);

            _healthText = UiKit.Shadowed(root, "HealthText", "100", 34, Palette.HudInk, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 84f), new Vector2(400f, 44f), out _healthShadow);

            _flaskRow = UiKit.Node(root, "Flasks");
            _flaskRow.anchorMin = new Vector2(0f, 0f);
            _flaskRow.anchorMax = new Vector2(0f, 0f);
            _flaskRow.pivot = new Vector2(0f, 0f);
            _flaskRow.anchoredPosition = new Vector2(36f, 4f);
            _flaskRow.sizeDelta = new Vector2(430f, 32f);

            _hintText = UiKit.Label(root, "Hint", "[F] FLASK", 18, Palette.HudDim, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(200f, 8f), new Vector2(400f, 26f));
        }

        void BuildBottomRight(Transform root)
        {
            _weaponText = UiKit.Shadowed(root, "Weapon", "", 30, Palette.HudInk, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 84f), new Vector2(700f, 40f), out _weaponShadow);

            _ammoText = UiKit.Shadowed(root, "Ammo", "", 54, Palette.HudGold, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 34f), new Vector2(700f, 62f), out _ammoShadow);

            _weaponListText = UiKit.Label(root, "WeaponList", "", 18, Palette.HudDim, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 8f), new Vector2(900f, 26f));
        }

        void BuildCentre(Transform root)
        {
            _comboText = UiKit.Label(root, "Combo", "", 34, Palette.HudGold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(700f, 46f));

            _announceText = UiKit.Banner(root, "Announce", "", 46, Palette.HudInk, 250f);
        }

        void BuildBossBar(Transform root)
        {
            _bossBar = UiKit.Node(root, "BossBar");
            _bossBar.anchorMin = new Vector2(0.5f, 1f);
            _bossBar.anchorMax = new Vector2(0.5f, 1f);
            _bossBar.pivot = new Vector2(0.5f, 1f);
            _bossBar.anchoredPosition = new Vector2(0f, -100f);
            _bossBar.sizeDelta = new Vector2(900f, 60f);

            UiKit.Rect(_bossBar, "BossBack", new Color(0.05f, 0.03f, 0.07f, 0.8f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 26f));

            _bossFill = UiKit.Rect(_bossBar, "BossFill", Palette.Lich,
                new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-448f, -24f), new Vector2(896f, 22f));

            _bossLabel = UiKit.Label(_bossBar, "BossLabel", "THE PALE ARCHIVIST", 24, Palette.Lich,
                TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 30f));

            _bossBar.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------- update

        void Update()
        {
            float dt = Time.deltaTime;

            UpdateCompass();
            UpdateReadouts();
            UpdateCrosshair(dt);
            UpdateFlash(dt);
            UpdateAnnounce(dt);
            UpdateBoss();
        }

        void UpdateCompass()
        {
            if (_player == null || _compassStrip == null) return;

            float yaw = _player.Yaw;
            float halfWidth = CompassWidth * 0.5f;

            for (int i = 0; i < _cardinalLabels.Count; i++)
            {
                float bearing = i * 45f;
                float delta = Mathf.DeltaAngle(yaw, bearing);
                bool inView = Mathf.Abs(delta) <= CompassHalfFov;

                _cardinalLabels[i].enabled = inView;
                if (inView)
                    _cardinalLabels[i].rectTransform.anchoredPosition =
                        new Vector2(delta / CompassHalfFov * halfWidth, 2f);
            }

            if (_objective != null)
            {
                float bearing = BearingTo(_objective.position);
                float delta = Mathf.DeltaAngle(yaw, bearing);
                bool inView = Mathf.Abs(delta) <= CompassHalfFov;

                _objectiveMarker.enabled = inView;
                if (inView)
                    _objectiveMarker.rectTransform.anchoredPosition =
                        new Vector2(delta / CompassHalfFov * halfWidth, 0f);

                float distance = Vector3.Distance(_player.transform.position, _objective.position);
                _objectiveText.text = _objectiveLabel + "   " + Mathf.RoundToInt(distance) + "M";
                _objectiveText.enabled = true;
            }
            else
            {
                _objectiveMarker.enabled = false;
                _objectiveText.enabled = false;
            }

            int used = 0;
            Vector3 playerPos = _player.transform.position;

            for (int i = 0; i < Enemy.Active.Count && used < MaxEnemyMarkers; i++)
            {
                var enemy = Enemy.Active[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float delta = Mathf.DeltaAngle(yaw, BearingTo(enemy.transform.position));
                if (Mathf.Abs(delta) > CompassHalfFov) continue;

                var marker = _enemyMarkers[used++];
                marker.enabled = true;
                marker.color = Palette.Of(enemy.Kind);

                float dist = Vector3.Distance(playerPos, enemy.transform.position);
                // Nearer enemies draw taller, so a wall of short ticks reads as "far away crowd"
                // and one tall tick reads as "something is on top of you".
                float height = Mathf.Lerp(24f, 9f, Mathf.Clamp01(dist / 45f));

                marker.rectTransform.sizeDelta = new Vector2(4f, height);
                marker.rectTransform.anchoredPosition = new Vector2(delta / CompassHalfFov * halfWidth, 0f);
            }

            for (int i = used; i < _enemyMarkers.Count; i++) _enemyMarkers[i].enabled = false;
        }

        float BearingTo(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - _player.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.001f) return _player.Yaw;

            return Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        }

        void UpdateReadouts()
        {
            if (_run == null) return;

            UiKit.SetText(_scoreText, _scoreShadow, UiKit.Money(_run.Score));
            _recordText.text = "RECORD  " + UiKit.Money(Mathf.Max(SaveSystem.Record, _run.Score));

            UiKit.SetText(_floorText, _floorShadow,
                "FLOOR " + _run.Floor + "    WAVE " + Mathf.Min(_run.Wave, _run.WavesThisFloor) + "/" + _run.WavesThisFloor);

            int remaining = _director != null ? _director.Alive + _director.Pending : 0;
            _threatText.text = "THREAT " + _run.ThreatLevel + "    FOES " + remaining;

            if (_run.Combo >= 3)
            {
                _comboFlash = Mathf.Max(0f, _comboFlash - Time.deltaTime * 3f);
                float scale = 1f + _comboFlash * 0.25f;

                _comboText.text = "x" + _run.ComboMultiplier + "   " + _run.Combo + " CHAIN";
                _comboText.color = Color.Lerp(Palette.HudGold, Color.white, _comboFlash);
                _comboText.rectTransform.localScale = Vector3.one * scale;
                _comboText.enabled = true;
            }
            else
            {
                _comboText.enabled = false;
            }

            if (_weapons != null && _weapons.Current != null)
            {
                var current = _weapons.Current;
                // The Spellslinger never runs dry, so it reads as an infinity mark rather than a
                // number the player would otherwise watch out of habit.
                UiKit.SetText(_ammoText, _ammoShadow, current.Def.InfiniteAmmo ? "∞" : current.Ammo.ToString());
                _ammoText.color = !current.Def.InfiniteAmmo && current.Ammo <= 5 ? Palette.HudBlood : Palette.HudGold;

                var list = "";
                for (int i = 0; i < _weapons.Weapons.Length; i++)
                {
                    var w = _weapons.Weapons[i];
                    if (!w.Unlocked) continue;

                    bool selected = w == current;
                    list += (selected ? "<color=#FFD15A>" : "<color=#5A6070>") + (i + 1) + " " + w.Def.ShortName + "</color>   ";
                }
                _weaponListText.text = list.TrimEnd();
            }
        }

        void UpdateCrosshair(float dt)
        {
            _crosshairKick = Mathf.Max(0f, _crosshairKick - dt * 5f);

            float spread = 10f;
            if (_weapons != null && _weapons.Current != null)
            {
                spread = 8f + _weapons.Current.Def.SpreadDegrees * 3.4f;
                if (Time.time < _weapons.Current.NextShotTime) _crosshairKick = Mathf.Max(_crosshairKick, 0.6f);
            }

            float radius = spread + _crosshairKick * 14f;

            _crosshair[0].rectTransform.anchoredPosition = new Vector2(0f, radius);
            _crosshair[1].rectTransform.anchoredPosition = new Vector2(0f, -radius);
            _crosshair[2].rectTransform.anchoredPosition = new Vector2(radius, 0f);
            _crosshair[3].rectTransform.anchoredPosition = new Vector2(-radius, 0f);

            if (_crosshairDot != null)
                _crosshairDot.color = new Color(1f, 0.9f, 0.5f, 0.6f + _crosshairKick * 0.4f);
        }

        void UpdateFlash(float dt)
        {
            if (_damageFlash == null) return;

            _flashAmount = Mathf.Max(0f, _flashAmount - dt * 2.6f);

            float lowHealth = 0f;
            if (_health != null && _health.MaxHealth > 0f)
            {
                float fraction = _health.Health / _health.MaxHealth;
                // A slow pulse below a third health, so the player feels the danger without
                // having to watch the bar.
                if (fraction < 0.34f && _health.IsAlive)
                    lowHealth = (0.14f + Mathf.Sin(Time.time * 5f) * 0.07f) * (1f - fraction / 0.34f);
            }

            var c = _damageFlash.color;
            c.a = Mathf.Clamp01(_flashAmount * 0.42f + lowHealth);
            _damageFlash.color = c;
        }

        void UpdateAnnounce(float dt)
        {
            if (_announceText == null) return;

            if (_announceTimer > 0f)
            {
                _announceTimer -= dt;
                float alpha = Mathf.Clamp01(_announceTimer / 0.6f);
                var c = Palette.HudInk;
                c.a = alpha;
                _announceText.color = c;
            }
            else if (_announceText.text.Length > 0)
            {
                _announceText.text = "";
            }
        }

        void UpdateBoss()
        {
            var boss = LichBoss.Current;
            bool show = boss != null && boss.IsAlive;

            if (_bossBar.gameObject.activeSelf != show) _bossBar.gameObject.SetActive(show);
            if (!show) return;

            float fraction = boss.HealthFraction;
            _bossFill.rectTransform.sizeDelta = new Vector2(896f * fraction, 22f);
            _bossFill.color = Color.Lerp(Palette.HudBlood, Palette.Lich, fraction);
        }

        // ------------------------------------------------------------------ events

        void HandleHealthChanged(float current, float max)
        {
            float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (_healthFill != null)
            {
                _healthFill.rectTransform.sizeDelta = new Vector2(426f * fraction, 30f);
                _healthFill.color = Color.Lerp(Palette.HudBlood, new Color(0.35f, 0.85f, 0.45f), fraction);
            }

            UiKit.SetText(_healthText, _healthShadow, Mathf.CeilToInt(current) + " / " + Mathf.RoundToInt(max));
        }

        void HandleFlasksChanged(int count, int capacity)
        {
            if (_flaskRow == null) return;

            while (_flaskIcons.Count < capacity)
            {
                var icon = UiKit.Rect(_flaskRow, "Flask" + _flaskIcons.Count, Palette.ShrineGlow,
                    new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(_flaskIcons.Count * 34f, 4f), new Vector2(26f, 26f));
                _flaskIcons.Add(icon);
            }

            for (int i = 0; i < _flaskIcons.Count; i++)
            {
                bool exists = i < capacity;
                _flaskIcons[i].enabled = exists;
                if (!exists) continue;

                _flaskIcons[i].color = i < count
                    ? Palette.ShrineGlow
                    : new Color(Palette.ShrineGlow.r, Palette.ShrineGlow.g, Palette.ShrineGlow.b, 0.18f);
            }

            if (_hintText != null)
                _hintText.text = capacity > 0 ? "[F] FLASK  " + count + "/" + capacity : "";
        }

        void HandleWeaponChanged(WeaponRuntime weapon)
        {
            if (weapon == null) return;

            UiKit.SetText(_weaponText, _weaponShadow, weapon.Def.Name);
            _weaponText.color = weapon.Def.Tint;
            _crosshairKick = 0.5f;
        }
    }
}
