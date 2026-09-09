using UnityEngine;

namespace Shmupper
{
    /// Gargoyles are the area denial. They are slow enough to run away from, so their threat is
    /// spatial: a slam that owns the ground near them and a thrown stone that punishes camping.
    public class GargoyleEnemy : GroundEnemy
    {
        enum State { Chase, SlamWindUp, Recover }

        State _state = State.Chase;
        float _timer;
        float _throwCooldown;

        const float SlamRange = 4.6f;
        const float SlamRadius = 6.2f;

        protected override void Behave(float dt)
        {
            _timer -= dt;
            _throwCooldown -= dt;

            float dist = DistanceToPlayer;

            switch (_state)
            {
                case State.Chase:
                    MoveAlong(ChaseDirection(), Speed, dt);
                    FaceMovement(PlayerPosition, dt, 4f);

                    if (dist < SlamRange)
                    {
                        _state = State.SlamWindUp;
                        _timer = 0.72f;
                        Fx.Ring(transform.position, SlamRadius, Palette.Gargoyle, 0.72f);
                        Sfx.Play(SfxId.UiDeny, transform.position, 0.5f, 0.7f);
                    }
                    else if (dist > 11f && _throwCooldown <= 0f && HasLineOfSight(transform.position + Vector3.up * 2f))
                    {
                        _throwCooldown = 2.6f;
                        ThrowStone();
                    }
                    break;

                case State.SlamWindUp:
                    MoveAlong(Vector3.zero, 0f, dt);
                    FaceMovement(PlayerPosition, dt, 3f);
                    if (_timer <= 0f) { Slam(); _state = State.Recover; _timer = 1.1f; }
                    break;

                default:
                    MoveAlong(Vector3.zero, 0f, dt);
                    if (_timer <= 0f) _state = State.Chase;
                    break;
            }
        }

        void Slam()
        {
            Vector3 center = transform.position;
            Fx.Explosion(center, SlamRadius, Palette.Gargoyle);
            Sfx.Play(SfxId.Explode, center, 0.7f);

            float dist = Vector3.Distance(center, PlayerPosition);
            if (dist > SlamRadius) return;

            Ctx.PlayerController?.Shake(0.55f, 0.4f);
            float falloff = Mathf.Clamp01(1f - dist / SlamRadius);
            HitPlayer(Damage * Mathf.Lerp(0.4f, 1f, falloff), 9f);
        }

        void ThrowStone()
        {
            Vector3 origin = transform.position + Vector3.up * 2.1f + transform.forward * 0.9f;
            Vector3 dir = (PlayerAimPoint - origin).normalized;

            Projectile.Spawn(origin, dir, false, 22f, Damage * 0.75f, 0.6f, Palette.Gargoyle, 3.2f, 5f, false);
            Sfx.Play(SfxId.ShootRocket, origin, 0.7f, 0.7f);
        }
    }
}
