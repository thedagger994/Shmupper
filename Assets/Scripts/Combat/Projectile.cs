using UnityEngine;

namespace Shmupper
{
    /// Bolts move by sphere-casting the distance they are about to travel rather than relying on
    /// trigger colliders. At the speeds this game fires at, physics triggers tunnel straight
    /// through walls and enemies; a cast per frame never does.
    public class Projectile : MonoBehaviour
    {
        public float Speed = 40f;
        public float Damage = 12f;
        public float Life = 6f;
        public float Radius = 0.22f;
        public float SplashRadius = 0f;
        public float PushForce = 0f;
        public bool FromPlayer = true;
        public Color Tint = Color.white;

        Transform _homingTarget;
        float _homingRate;
        int _mask;
        float _age;
        Transform _trail;
        Vector3 _trailBase;

        public void Setup(Vector3 position, Vector3 direction, bool fromPlayer)
        {
            transform.position = position;
            transform.forward = direction.normalized;
            FromPlayer = fromPlayer;
            _mask = fromPlayer ? Layers.ShotMask : Layers.EnemyShotMask;
            _age = 0f;
        }

        public void SetHoming(Transform target, float rate)
        {
            _homingTarget = target;
            _homingRate = rate;
        }

        public void BuildVisual(float size, Color color, bool spiky)
        {
            Tint = color;
            var mat = MatLib.Lit(color * 0.3f, color * 3.5f);

            Shapes.Ball(transform, Vector3.zero, size, mat);

            if (spiky)
            {
                Shapes.Box(transform, Vector3.zero, new Vector3(size * 0.35f, size * 0.35f, size * 2.4f), mat);
                Shapes.Box(transform, Vector3.zero, new Vector3(size * 2.2f, size * 0.3f, size * 0.3f), mat);
            }

            // Only the heavy ordnance carries a real light. The Lich alone can have forty bolts
            // in the air at once, and forty point lights would cost more than the whole rest of
            // the frame; the small bolts rely on emission and bloom instead.
            if (size >= 0.4f)
            {
                var light = gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = 7f;
                light.intensity = 2.2f;
                light.shadows = LightShadows.None;
            }

            _trailBase = new Vector3(size * 0.5f, size * 0.5f, size * 2f);
            _trail = Shapes.Box(transform, Vector3.back * size * 1.2f, _trailBase, mat).transform;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age > Life) { Destroy(gameObject); return; }

            if (_homingTarget != null && _homingRate > 0f)
            {
                Vector3 want = (_homingTarget.position + Vector3.up * 0.6f - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(transform.forward, want, _homingRate * Time.deltaTime, 0f);
            }

            float step = Speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, Radius, transform.forward, out RaycastHit hit, step,
                    _mask, QueryTriggerInteraction.Ignore))
            {
                Explode(hit.point, hit.normal, hit.collider);
                return;
            }

            transform.position += transform.forward * step;

            if (_trail != null)
            {
                float wobble = 1f + Mathf.Sin(Time.time * 30f) * 0.3f;
                _trail.localScale = new Vector3(_trailBase.x, _trailBase.y, _trailBase.z * wobble);
            }
        }

        void Explode(Vector3 point, Vector3 normal, Collider direct)
        {
            if (SplashRadius > 0.01f)
            {
                Fx.Explosion(point, SplashRadius, Tint);
                Sfx.Play(SfxId.Explode, point);

                int mask = FromPlayer ? Layers.EnemyMask : Layers.PlayerMask;
                var hits = Physics.OverlapSphere(point, SplashRadius, mask, QueryTriggerInteraction.Ignore);

                var alreadyHit = new System.Collections.Generic.HashSet<IDamageable>();
                foreach (var col in hits)
                {
                    var d = DamageUtil.Find(col);
                    if (d == null || !d.IsAlive || !alreadyHit.Add(d)) continue;

                    float dist = Vector3.Distance(point, d.Transform.position);
                    float falloff = Mathf.Clamp01(1f - (dist / SplashRadius));

                    d.TakeDamage(new DamageInfo
                    {
                        Amount = Damage * Mathf.Lerp(0.35f, 1f, falloff),
                        Point = point,
                        Normal = (d.Transform.position - point).normalized,
                        Push = (d.Transform.position - point).normalized * PushForce * falloff,
                        FromPlayer = FromPlayer,
                        IsExplosive = true
                    });
                }
            }
            else
            {
                Fx.Impact(point, normal, Tint);
                Sfx.Play(SfxId.Impact, point);

                var d = DamageUtil.Find(direct);
                if (d != null && d.IsAlive)
                {
                    d.TakeDamage(new DamageInfo
                    {
                        Amount = Damage,
                        Point = point,
                        Normal = normal,
                        Push = transform.forward * PushForce,
                        FromPlayer = FromPlayer
                    });
                }
            }

            Destroy(gameObject);
        }

        /// Single entry point every weapon and every caster uses, so projectile setup can never
        /// drift between the player and the enemies that mirror it.
        public static Projectile Spawn(Vector3 position, Vector3 direction, bool fromPlayer,
                                       float speed, float damage, float size, Color color,
                                       float splash = 0f, float push = 0f, bool spiky = false, float life = 6f)
        {
            var go = new GameObject(fromPlayer ? "PlayerBolt" : "EnemyBolt");
            go.layer = Layers.Projectile;

            var p = go.AddComponent<Projectile>();
            p.Speed = speed;
            p.Damage = damage;
            p.SplashRadius = splash;
            p.PushForce = push;
            p.Radius = size * 0.5f;
            p.Life = life;
            p.Setup(position, direction, fromPlayer);
            p.BuildVisual(size, color, spiky);

            return p;
        }
    }
}
