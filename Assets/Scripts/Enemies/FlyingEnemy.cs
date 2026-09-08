using UnityEngine;

namespace Shmupper
{
    /// Base for anything that ignores the floor plan. Fliers steer straight at a desired point
    /// and slide along whatever they bump into, which is enough in a castle of straight walls and
    /// keeps them feeling weightless next to the walkers.
    public abstract class FlyingEnemy : Enemy
    {
        protected Vector3 Velocity;
        protected float DesiredAltitude = 4.2f;

        SphereCollider _collider;

        protected override void ConfigureCollision()
        {
            _collider = gameObject.AddComponent<SphereCollider>();
            _collider.radius = Stats.Radius;
            _collider.center = Vector3.up * 0.2f;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected void Steer(Vector3 desiredPosition, float speed, float dt, float responsiveness = 3.2f)
        {
            Vector3 desired = desiredPosition - transform.position;
            float dist = desired.magnitude;

            Vector3 wanted = dist > 0.1f ? desired / dist * Mathf.Min(speed, dist * 2.2f) : Vector3.zero;
            Velocity = Vector3.Lerp(Velocity, wanted, Mathf.Clamp01(dt * responsiveness));

            Vector3 step = Velocity * dt;
            float stepLength = step.magnitude;

            if (stepLength > 0.0001f &&
                Physics.SphereCast(transform.position, Stats.Radius, step / stepLength, out RaycastHit hit,
                    stepLength + 0.25f, Layers.LevelMask, QueryTriggerInteraction.Ignore))
            {
                // Project the motion onto the surface so a dragon skims along a wall instead of
                // grinding to a halt against it.
                Velocity = Vector3.ProjectOnPlane(Velocity, hit.normal);
                step = Velocity * dt;
            }

            transform.position += step;
            ClampToRoom();
        }

        void ClampToRoom()
        {
            Vector3 p = transform.position;
            float floorY = 0f;

            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit down, 12f,
                    Layers.LevelMask, QueryTriggerInteraction.Ignore))
                floorY = down.point.y;

            float minY = floorY + 1.1f;
            float maxY = floorY + DungeonMap.WallHeight - 1.0f;

            p.y = Mathf.Clamp(p.y, minY, maxY);
            transform.position = p;
        }

        protected void FaceTarget(Vector3 target, float dt, float turnSpeed = 7f)
        {
            Vector3 look = target - transform.position;
            if (look.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), dt * turnSpeed);
        }

        /// A point on a slowly rotating circle around the player. Every flier gets its own phase
        /// so a squadron spreads around the arena rather than stacking on one side of it.
        protected Vector3 OrbitPoint(float radius, float height, float phase, float rate)
        {
            float angle = Time.time * rate + phase;
            return PlayerPosition + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }

        protected void HitPlayer(float damage, float knockback)
        {
            if (Ctx.PlayerHealth == null || !Ctx.PlayerHealth.IsAlive) return;

            Vector3 dir = (PlayerPosition - transform.position).normalized;
            Ctx.PlayerHealth.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Point = PlayerAimPoint,
                Normal = -dir,
                Push = dir * knockback,
                FromPlayer = false
            });
        }
    }
}
