using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Owns the horde: what spawns, where, and how fast.
    ///
    /// Two rules shape it. Enemies trickle in rather than appearing all at once, so a wave builds
    /// pressure instead of dumping it; and spawns are pushed away from the player and preferably
    /// out of sight, because a shmup is only fair when everything that kills you was visible on
    /// its way in.
    public class WaveDirector : MonoBehaviour
    {
        public event Action<int, int> OnWaveStarted;
        public event Action<int> OnWaveCleared;
        public event Action OnFloorCleared;
        public event Action<Enemy, int> OnEnemyKilled;
        public event Action<int> OnAliveCountChanged;

        const int MaxConcurrent = 26;

        EnemyContext _ctx;
        DungeonMap _map;
        RunState _run;
        Transform _player;
        PlayerHealth _playerHealth;
        WeaponSystem _weapons;
        Transform _holder;

        readonly List<EnemyKind> _pending = new List<EnemyKind>();
        float _spawnTimer;
        int _aliveCount;
        bool _waveRunning;
        bool _bossWave;

        public int Alive => _aliveCount;
        public int Pending => _pending.Count;
        public bool WaveRunning => _waveRunning;

        public void Init(RunState run, Transform player, PlayerHealth playerHealth,
                         PlayerController playerController, WeaponSystem weapons)
        {
            _run = run;
            _player = player;
            _playerHealth = playerHealth;
            _weapons = weapons;

            _ctx = new EnemyContext
            {
                Player = player,
                PlayerHealth = playerHealth,
                PlayerController = playerController,
                Run = run,
                OnEnemyDied = HandleEnemyDied,
                SpawnEnemy = (kind, pos) => SpawnEnemy(kind, pos)
            };
        }

        public void SetRun(RunState run)
        {
            _run = run;
            if (_ctx != null) _ctx.Run = run;
        }

        public void SetFloor(DungeonMap map, FlowField flow)
        {
            _map = map;
            _ctx.Map = map;
            _ctx.Flow = flow;

            ClearAll();

            if (_holder != null) Destroy(_holder.gameObject);
            var holderGo = new GameObject("Enemies");
            _holder = holderGo.transform;
        }

        public void ClearAll()
        {
            for (int i = Enemy.Active.Count - 1; i >= 0; i--)
            {
                var e = Enemy.Active[i];
                if (e != null) Destroy(e.gameObject);
            }

            Enemy.Active.Clear();
            _pending.Clear();
            _aliveCount = 0;
            _waveRunning = false;
            OnAliveCountChanged?.Invoke(0);
        }

        public void StartWave(int wave)
        {
            _pending.Clear();
            _bossWave = IsBossWave(wave);

            BuildRoster(wave);

            _waveRunning = true;
            _spawnTimer = 0.6f;

            Sfx.Play2D(SfxId.WaveStart);
            OnWaveStarted?.Invoke(wave, _run.WavesThisFloor);
        }

        bool IsBossWave(int wave) => wave >= _run.WavesThisFloor && _run.Floor % 3 == 0;

        /// The roster is a budget, not a fixed list: cheap enemies fill the gaps left by the
        /// expensive ones so wave size grows smoothly rather than in steps.
        void BuildRoster(int wave)
        {
            int budget = 7 + wave * 3 + (_run.Floor - 1) * 3 + _run.EnemyCountBonus;
            int floor = _run.Floor;

            var pool = new List<EnemyKind> { EnemyKind.Imp, EnemyKind.HollowKnight };
            if (floor >= 2 || wave >= 3) pool.Add(EnemyKind.Wizard);
            if (floor >= 3) pool.Add(EnemyKind.BabyDragon);
            if (floor >= 4) pool.Add(EnemyKind.Gargoyle);

            if (_bossWave)
            {
                _pending.Add(EnemyKind.Lich);
                budget = Mathf.RoundToInt(budget * 0.55f);
            }

            int guard = 0;
            while (budget > 0 && guard++ < 400)
            {
                var kind = pool[UnityEngine.Random.Range(0, pool.Count)];
                int cost = CostOf(kind);
                if (cost > budget && kind != EnemyKind.Imp) continue;

                _pending.Add(kind);
                budget -= cost;
            }

            Shuffle(_pending);
        }

        static int CostOf(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Imp: return 1;
                case EnemyKind.HollowKnight: return 3;
                case EnemyKind.Wizard: return 4;
                case EnemyKind.BabyDragon: return 5;
                case EnemyKind.Gargoyle: return 7;
                default: return 25;
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        void Update()
        {
            if (!_waveRunning || _map == null) return;

            if (_pending.Count > 0 && _aliveCount < MaxConcurrent)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = UnityEngine.Random.Range(0.22f, 0.5f);
                    SpawnNext();
                }
            }

            if (_pending.Count == 0 && _aliveCount <= 0)
            {
                _waveRunning = false;
                OnWaveCleared?.Invoke(_run.Wave);

                if (_run.Wave >= _run.WavesThisFloor) OnFloorCleared?.Invoke();
            }
        }

        void SpawnNext()
        {
            var kind = _pending[0];
            _pending.RemoveAt(0);

            Vector3 position = PickSpawnPoint(kind);
            SpawnEnemy(kind, position);
        }

        Vector3 PickSpawnPoint(EnemyKind kind)
        {
            if (_map == null || _map.SpawnCells.Count == 0) return Vector3.zero;

            Vector3 playerPos = _player != null ? _player.position : Vector3.zero;
            float height = kind == EnemyKind.BabyDragon || kind == EnemyKind.Lich ? 3.2f : 0.4f;

            Vector3 fallback = Vector3.zero;
            float bestScore = float.MinValue;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var cell = _map.SpawnCells[UnityEngine.Random.Range(0, _map.SpawnCells.Count)];
                Vector3 candidate = _map.CellToWorld(cell, height);

                float dist = Vector3.Distance(candidate, playerPos);
                if (dist < 13f) continue;

                bool visible = !Physics.Linecast(playerPos + Vector3.up * 1.2f, candidate + Vector3.up,
                    Layers.LevelMask, QueryTriggerInteraction.Ignore);

                // Prefer a spot the player cannot currently see, but never so far away that the
                // wave stalls while the horde walks across the whole floor.
                float score = -Mathf.Abs(dist - 22f) + (visible ? -14f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    fallback = candidate;
                }
            }

            if (bestScore > float.MinValue) return fallback;

            var any = _map.SpawnCells[UnityEngine.Random.Range(0, _map.SpawnCells.Count)];
            return _map.CellToWorld(any, height);
        }

        public Enemy SpawnEnemy(EnemyKind kind, Vector3 position)
        {
            var go = new GameObject("Enemy_" + kind);
            if (_holder != null) go.transform.SetParent(_holder, true);

            Enemy enemy = AttachBehaviour(go, kind);
            enemy.Spawn(_ctx, kind, position);

            _aliveCount++;
            OnAliveCountChanged?.Invoke(_aliveCount);
            return enemy;
        }

        static Enemy AttachBehaviour(GameObject go, EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Imp:          return go.AddComponent<ImpEnemy>();
                case EnemyKind.HollowKnight: return go.AddComponent<HollowKnightEnemy>();
                case EnemyKind.Wizard:       return go.AddComponent<WizardEnemy>();
                case EnemyKind.BabyDragon:   return go.AddComponent<BabyDragonEnemy>();
                case EnemyKind.Gargoyle:     return go.AddComponent<GargoyleEnemy>();
                default:                     return go.AddComponent<LichBoss>();
            }
        }

        void HandleEnemyDied(Enemy enemy)
        {
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            OnAliveCountChanged?.Invoke(_aliveCount);

            int points = _run.RegisterKill(enemy.Points);
            OnEnemyKilled?.Invoke(enemy, points);

            if (_run.SiphonPerKill > 0f) _playerHealth?.Heal(_run.SiphonPerKill);

            DropLoot(enemy);
        }

        void DropLoot(Enemy enemy)
        {
            Vector3 at = enemy.transform.position;
            float roll = UnityEngine.Random.value;

            bool flaskUseful = _playerHealth != null && _playerHealth.Flasks < _playerHealth.FlaskCapacity;
            float flaskChance = enemy.Kind == EnemyKind.Lich ? 1f : (flaskUseful ? 0.10f : 0f);

            if (roll < flaskChance)
            {
                Pickup.Spawn(PickupKind.Flask, at, _player, _playerHealth, _weapons);
                return;
            }

            if (roll < flaskChance + 0.26f)
                Pickup.Spawn(PickupKind.Ammo, at, _player, _playerHealth, _weapons);
        }
    }
}
