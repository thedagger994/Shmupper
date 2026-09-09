using UnityEngine;

namespace Shmupper
{
    /// Imps are the pressure. Individually trivial, they arrive in numbers, move faster than the
    /// player can walk, and exist to stop anyone standing still and aiming carefully.
    public class ImpEnemy : GroundEnemy
    {
        float _leapCooldown;

        protected override void Behave(float dt)
        {
            AttackCooldown -= dt;
            _leapCooldown -= dt;

            float dist = DistanceToPlayer;
            Vector3 dir = ChaseDirection();

            // A short hop closes the last few metres and makes imps genuinely awkward to track,
            // which is the only threat a 34 hit point enemy can offer.
            if (_leapCooldown <= 0f && dist < 9f && dist > 3f && Controller.isGrounded && HasLineOfSight(transform.position + Vector3.up))
            {
                _leapCooldown = Random.Range(2.2f, 3.8f);
                VerticalVelocity = 7.5f;
            }

            MoveAlong(dir, Speed, dt);
            FaceMovement(PlayerPosition, dt, 12f);

            if (dist < 1.9f && AttackCooldown <= 0f)
            {
                AttackCooldown = 0.85f;
                HitPlayer(Damage, 3f);
                Fx.Impact(PlayerAimPoint, (transform.position - PlayerPosition).normalized, Palette.Imp, 4, 0.7f);
            }
        }
    }
}
