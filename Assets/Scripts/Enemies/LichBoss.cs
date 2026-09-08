using UnityEngine;

namespace Shmupper
{
    /// The floor guardian. The Lich exists to test everything the floor taught: a radial volley
    /// that must be dodged laterally, summons that must be cleared fast, and a channelled beam
    /// that must be broken with cover. It never chases hard - it makes the arena the problem.
    public class LichBoss : FlyingEnemy
    {
        enum State { Drift, Volley, Summon, Beam, Blink }

        public static LichBoss Current { get; private set; }

        State _state = State.Drift;
        float _timer;
        float _stateCooldown;
        float _phase;
        int _volleyLeft;
        float _volleyTimer;
        float _spin;
        float _beamTick;
        Transform _beamVisual;
        int _nextBlinkThreshold = 3;

        protected override void OnSpawned()
        {
            Current = this;
            _phase = Random.value * 6f;
            DesiredAltitude = 3.4f;
            _stateCooldown = 1.6f;
        }

        protected override void Behave(float dt)
        {
            _timer -= dt;
            _stateCooldown -= dt;

            switch (_state)
            {
                case State.Drift:   Drift(dt); break;
                case State.Volley:  Volley(dt); break;
                case State.Summon:  Summon(dt); break;
                case State.Beam:    Beam(dt); break;
                default:            BlinkState(dt); break;
            }
        }

        void Drift(float dt)
        {
            Steer(OrbitPoint(13f, DesiredAltitude, _phase, 0.28f), Speed, dt, 2.2f);
            FaceTarget(PlayerAimPoint, dt, 4f);

            if (_stateCooldown > 0f) return;

            // Weight the choice toward whatever the player is least prepared for: summons when
            // the arena is empty, the beam when they are close, volleys otherwise.
            int living = CountMinions();
            float dist = DistanceToPlayer;

            if (living <= 1 && Random.value < 0.55f) EnterSummon();
            else if (dist < 16f && HasLineOfSight(transform.position) && Random.value < 0.45f) EnterBeam();
            else EnterVolley();
        }

        void EnterVolley()
        {
            _state = State.Volley;
            _volleyLeft = 5;
            _volleyTimer = 0.25f;
            _timer = 4f;
            Sfx.Play(SfxId.Shrine, transform.position, 0.7f, 0.7f);
        }

        void Volley(float dt)
        {
            Steer(OrbitPoint(12f, DesiredAltitude + 0.8f, _phase, 0.5f), Speed * 0.8f, dt, 2.4f);
            FaceTarget(PlayerAimPoint, dt, 6f);

            _volleyTimer -= dt;
            if (_volleyTimer > 0f) return;

            _volleyTimer = 0.42f;
            _volleyLeft--;
            FireRing();

            if (_volleyLeft <= 0) LeaveState(1.6f);
        }

        /// Eight bolts on a slowly rotating ring. The rotation is what makes consecutive volleys
        /// readable but not memorisable.
        void FireRing()
        {
            _spin += 22f;
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 toPlayer = (PlayerAimPoint - origin).normalized;

            for (int i = 0; i < 8; i++)
            {
                float angle = _spin + i * 45f;
                Vector3 dir = Quaternion.AngleAxis(angle, toPlayer) * Vector3.Cross(toPlayer, Vector3.up).normalized;
                dir = (toPlayer * 2.4f + dir).normalized;

                Projectile.Spawn(origin + dir * 1.2f, dir, false, 19f, Damage * 0.55f, 0.34f,
                    Palette.Wizard, 0f, 2f, true, 5.5f);
            }

            Sfx.Play(SfxId.ShootFlux, origin, 0.55f, 0.8f);
        }

        void EnterSummon()
        {
            _state = State.Summon;
            _timer = 1.0f;
            Fx.Ring(transform.position, 6f, Palette.Lich, 1.0f);
            Sfx.Play(SfxId.Shrine, transform.position, 0.5f);
        }

        void Summon(float dt)
        {
            Steer(transform.position + Vector3.up * 0.4f, Speed * 0.4f, dt, 2f);
            FaceTarget(PlayerAimPoint, dt, 3f);

            if (_timer > 0f) return;

            if (Ctx.SpawnEnemy != null)
            {
                int count = 3 + Ctx.Run.Floor / 2;
                for (int i = 0; i < count; i++)
                {
                    float a = i / (float)count * Mathf.PI * 2f;
                    Vector3 at = transform.position + new Vector3(Mathf.Cos(a) * 3.4f, -1.6f, Mathf.Sin(a) * 3.4f);
                    var cell = Ctx.Map.NearestOpen(at);
                    Ctx.SpawnEnemy(i % 3 == 2 ? EnemyKind.HollowKnight : EnemyKind.Imp, Ctx.Map.CellToWorld(cell, 0.4f));
                }
            }

            LeaveState(2.4f);
        }

        void EnterBeam()
        {
            _state = State.Beam;
            _timer = 2.4f;
            _beamTick = 0f;

            var go = Shapes.Box(transform, new Vector3(0f, 1.4f, 20f), new Vector3(0.35f, 0.35f, 40f),
                MatLib.Lit(Palette.Lich * 0.3f, Palette.Lich * 3.5f));
            go.name = "Beam";
            _beamVisual = go.transform;

            Sfx.Play(SfxId.ShootRocket, transform.position, 0.4f, 0.9f);
        }

        void Beam(float dt)
        {
            Steer(transform.position, Speed * 0.2f, dt, 1.5f);
            FaceTarget(PlayerAimPoint, dt, 1.6f);

            _beamTick -= dt;
            if (_beamTick <= 0f)
            {
                _beamTick = 0.22f;

                Vector3 origin = transform.position + Vector3.up * 1.4f;
                if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, 45f,
                        Layers.EnemyShotMask, QueryTriggerInteraction.Ignore))
                {
                    var target = DamageUtil.Find(hit.collider);
                    if (target is PlayerHealth) HitPlayer(Damage * 0.22f, 0.6f);

                    if (_beamVisual != null)
                    {
                        float len = Vector3.Distance(origin, hit.point);
                        _beamVisual.localScale = new Vector3(0.35f, 0.35f, len);
                        _beamVisual.localPosition = new Vector3(0f, 1.4f, len * 0.5f);
                    }
                }
            }

            if (_timer <= 0f)
            {
                if (_beamVisual != null) Destroy(_beamVisual.gameObject);
                LeaveState(2.0f);
            }
        }

        void BlinkState(float dt)
        {
            Steer(transform.position, 0f, dt, 8f);
            if (_timer > 0f) return;

            var map = Ctx.Map;
            if (map != null && map.SpawnCells.Count > 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    var cell = map.SpawnCells[Random.Range(0, map.SpawnCells.Count)];
                    Vector3 candidate = map.CellToWorld(cell, DesiredAltitude);
                    float d = Vector3.Distance(candidate, PlayerPosition);
                    if (d < 11f || d > 26f) continue;

                    Fx.Burst(transform.position, Palette.Lich, 5f, 0.4f);
                    transform.position = candidate;
                    Fx.Burst(candidate, Palette.Lich, 5f, 0.4f);
                    break;
                }
            }

            LeaveState(0.8f);
        }

        void LeaveState(float cooldown)
        {
            _state = State.Drift;
            _stateCooldown = cooldown;
        }

        protected override void OnHurt(DamageInfo info)
        {
            // Escape hatches at each quarter of its health keep the fight from turning into a
            // corner-and-shred once the player has a rocket launcher.
            int quarter = Mathf.CeilToInt(HealthFraction * 4f);
            if (quarter >= _nextBlinkThreshold) return;

            _nextBlinkThreshold = quarter;
            if (_beamVisual != null) Destroy(_beamVisual.gameObject);

            _state = State.Blink;
            _timer = 0.25f;
        }

        int CountMinions()
        {
            int n = 0;
            foreach (var e in Active)
                if (e != null && e != this && e.IsAlive) n++;
            return n;
        }

        protected override void Die(bool byPlayer)
        {
            if (_beamVisual != null) Destroy(_beamVisual.gameObject);
            if (Current == this) Current = null;

            Fx.Explosion(transform.position + Vector3.up, 9f, Palette.Lich);
            Ctx.PlayerController?.Shake(0.9f, 0.8f);

            base.Die(byPlayer);
        }

        protected override void OnDestroy()
        {
            if (Current == this) Current = null;
            base.OnDestroy();
        }
    }
}
