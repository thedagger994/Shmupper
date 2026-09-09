using UnityEngine;

namespace Shmupper
{
    /// Everything that walks. Steering is the flow field plus a short-range separation push, so
    /// a pack funnelling down a corridor spreads across its width instead of collapsing into one
    /// conga line.
    public abstract class GroundEnemy : Enemy
    {
        protected CharacterController Controller;
        protected float VerticalVelocity;
        protected float AttackCooldown;

        static readonly Collider[] NeighbourBuffer = new Collider[12];

        /// Idempotent: a forged prefab already carries its controller, so this reuses whatever is
        /// there and only adds one when the enemy was created from bare code.
        protected override void ConfigureCollision()
        {
            Controller = GetComponent<CharacterController>();
            if (Controller == null) Controller = gameObject.AddComponent<CharacterController>();

            Controller.radius = Stats.Radius;
            Controller.height = Stats.Height;
            Controller.center = new Vector3(0f, Stats.Height * 0.5f, 0f);
            Controller.slopeLimit = 55f;
            Controller.stepOffset = 0.6f;
        }

        protected void MoveAlong(Vector3 direction, float speed, float dt)
        {
            Vector3 move = direction;
            move.y = 0f;

            if (move.sqrMagnitude > 0.001f) move = move.normalized * speed;

            move += Separation() * (speed * 0.65f);

            if (Controller.isGrounded && VerticalVelocity < 0f) VerticalVelocity = -2f;
            else VerticalVelocity -= 26f * dt;

            move.y = VerticalVelocity;
            Controller.Move(move * dt);
        }

        protected Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            float radius = Stats.Radius * 2.4f;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, NeighbourBuffer,
                Layers.EnemyMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var col = NeighbourBuffer[i];
                if (col == null || col.transform == transform) continue;

                Vector3 delta = transform.position - col.transform.position;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.01f)
                {
                    push += new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);
                    continue;
                }

                if (dist < radius) push += delta / dist * (1f - dist / radius);
            }

            return Vector3.ClampMagnitude(push, 1f);
        }

        protected void FaceMovement(Vector3 target, float dt, float turnSpeed = 9f)
        {
            Vector3 look = target - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(look), dt * turnSpeed);
        }

        protected Vector3 ChaseDirection()
        {
            // Close in, a direct line beats the grid: the flow field snaps to cell centres and
            // would make an enemy standing next to the player shuffle sideways.
            if (DistanceToPlayer < 6f)
            {
                Vector3 direct = PlayerPosition - transform.position;
                direct.y = 0f;
                if (direct.sqrMagnitude > 0.01f && HasLineOfSight(transform.position + Vector3.up * 1.2f))
                    return direct.normalized;
            }

            return Ctx.Flow != null ? Ctx.Flow.DirectionAt(transform.position) : Vector3.zero;
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
