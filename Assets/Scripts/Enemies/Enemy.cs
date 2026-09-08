using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Shared behaviour for everything that wants the Gunmage dead: health, hit feedback, death
    /// rewards, and the spawn materialisation. Subclasses only implement how they move and how
    /// they attack.
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        public EnemyKind Kind { get; protected set; }

        protected EnemyContext Ctx;
        protected EnemyStats Stats;
        protected float MaxHealth;
        protected float CurrentHealth;
        protected Transform Body;

        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Color> _baseColors = new List<Color>();
        MaterialPropertyBlock _mpb;

        float _flashTimer;
        float _spawnTimer;
        bool _dead;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        /// Live roster, used for crowd separation and for the compass. Kept as a plain static
        /// list because it is walked every frame and a scene search would not be affordable.
        public static readonly List<Enemy> Active = new List<Enemy>();

        public bool IsAlive => !_dead && CurrentHealth > 0f;
        public Transform Transform => transform;
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(CurrentHealth / MaxHealth) : 0f;

        /// Enemies fade in over their first half second and cannot act or be hit during it. This
        /// stops the player from being instantly bitten by something that materialised behind
        /// them, which in a game with this many spawns would feel like cheating.
        protected bool IsMaterialising => _spawnTimer > 0f;

        public void Spawn(EnemyContext ctx, EnemyKind kind, Vector3 position)
        {
            Ctx = ctx;
            Kind = kind;
            Stats = EnemyStats.For(kind);

            MaxHealth = Stats.Health * ctx.Run.EnemyHealthMul;
            CurrentHealth = MaxHealth;

            transform.position = position;
            gameObject.layer = Layers.Enemy;
            _mpb = new MaterialPropertyBlock();
            _spawnTimer = 0.55f;

            BuildBody();
            CacheRenderers();
            ConfigureCollision();
            OnSpawned();

            Active.Add(this);

            Fx.Burst(position + Vector3.up * Stats.Height * 0.5f, Palette.Of(kind), Stats.Radius * 4f, 0.4f);
            Sfx.Play(SfxId.Pickup, position, 0.6f, 0.5f);
        }

        protected virtual void BuildBody()
        {
            Body = EnemyBuilder.Build(Kind, transform);
        }

        protected abstract void ConfigureCollision();

        protected virtual void OnSpawned() { }

        protected abstract void Behave(float dt);

        protected float Damage => Stats.Damage * Ctx.Run.EnemyDamageMul;
        protected float Speed => Stats.Speed * Ctx.Run.EnemySpeedMul;

        protected Vector3 PlayerPosition => Ctx.Player != null ? Ctx.Player.position : transform.position;

        protected Vector3 PlayerAimPoint => PlayerPosition + Vector3.up * 0.9f;

        protected float DistanceToPlayer => Vector3.Distance(transform.position, PlayerPosition);

        protected bool HasLineOfSight(Vector3 from)
        {
            Vector3 delta = PlayerAimPoint - from;
            float dist = delta.magnitude;
            if (dist < 0.2f) return true;

            return !Physics.Raycast(from, delta / dist, dist - 0.4f, Layers.SightMask, QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            if (_dead) return;

            float dt = Time.deltaTime;

            if (_spawnTimer > 0f)
            {
                _spawnTimer -= dt;
                float t = Mathf.Clamp01(1f - (_spawnTimer / 0.55f));
                if (Body != null) Body.localScale = Vector3.one * Mathf.SmoothStep(0.1f, 1f, t);
                if (_spawnTimer > 0f) return;
                if (Body != null) Body.localScale = Vector3.one;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                ApplyFlash(Mathf.Clamp01(_flashTimer / 0.09f));
            }

            Behave(dt);
        }

        public virtual void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || IsMaterialising) return;

            CurrentHealth -= info.Amount;
            _flashTimer = 0.09f;

            Fx.Impact(info.Point, info.Normal, Palette.Of(Kind), 3, 0.6f);
            Sfx.Play(SfxId.EnemyHurt, transform.position, 1f, 0.5f);

            OnHurt(info);

            if (CurrentHealth <= 0f) Die(info.FromPlayer);
        }

        protected virtual void OnHurt(DamageInfo info) { }

        protected virtual void Die(bool byPlayer)
        {
            if (_dead) return;
            _dead = true;

            Fx.KillSpark(transform.position + Vector3.up * Stats.Height * 0.5f, Palette.Of(Kind));
            Sfx.Play(SfxId.EnemyDie, transform.position);

            Ctx.OnEnemyDied?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            Active.Remove(this);
        }

        public int Points => Stats.Points;

        // ------------------------------------------------------------- hit feedback

        void CacheRenderers()
        {
            _renderers.Clear();
            _baseColors.Clear();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                _renderers.Add(r);
                var mat = r.sharedMaterial;
                Color c = Color.white;
                if (mat != null)
                {
                    if (mat.HasProperty(BaseColorId)) c = mat.GetColor(BaseColorId);
                    else if (mat.HasProperty(ColorId)) c = mat.GetColor(ColorId);
                }
                _baseColors.Add(c);
            }
        }

        /// Materials are shared across the whole horde, so the hit flash has to go through a
        /// property block. Writing to the material directly would flash every enemy of that kind
        /// at once.
        void ApplyFlash(float amount)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                r.GetPropertyBlock(_mpb);
                Color c = Color.Lerp(_baseColors[i], Color.white, amount);
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
