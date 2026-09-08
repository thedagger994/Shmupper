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

    /// Hollow knights are the wall. Slow to commit but heavily armoured, they punish anyone who
    /// backpedals in a straight line and force the player to move around them.
    public class HollowKnightEnemy : GroundEnemy
    {
        enum State { Chase, WindUp, Strike, Recover }

        State _state = State.Chase;
        float _timer;
        Vector3 _lungeDirection;

        const float StrikeRange = 3.4f;

        protected override void Behave(float dt)
        {
            _timer -= dt;
            float dist = DistanceToPlayer;

            switch (_state)
            {
                case State.Chase:
                    MoveAlong(ChaseDirection(), Speed, dt);
                    FaceMovement(PlayerPosition, dt);

                    if (dist < StrikeRange - 0.5f && HasLineOfSight(transform.position + Vector3.up * 1.4f))
                    {
                        _state = State.WindUp;
                        _timer = 0.48f;
                        Fx.Ring(transform.position, StrikeRange, Palette.HollowKnight, 0.48f);
                        Sfx.Play(SfxId.UiDeny, transform.position, 0.7f, 0.5f);
                    }
                    break;

                case State.WindUp:
                    MoveAlong(Vector3.zero, 0f, dt);
                    FaceMovement(PlayerPosition, dt, 5f);

                    if (_timer <= 0f)
                    {
                        _state = State.Strike;
                        _timer = 0.22f;
                        _lungeDirection = transform.forward;
                        DoStrike();
                    }
                    break;

                case State.Strike:
                    MoveAlong(_lungeDirection, Speed * 2.4f, dt);
                    if (_timer <= 0f) { _state = State.Recover; _timer = 0.55f; }
                    break;

                default:
                    MoveAlong(Vector3.zero, 0f, dt);
                    FaceMovement(PlayerPosition, dt, 4f);
                    if (_timer <= 0f) _state = State.Chase;
                    break;
            }
        }

        void DoStrike()
        {
            Sfx.Play(SfxId.ShootShotgun, transform.position, 0.7f, 0.6f);
            Fx.Impact(transform.position + transform.forward * 1.6f + Vector3.up, transform.forward,
                Palette.HollowKnight, 8, 1.1f);

            Vector3 toPlayer = PlayerPosition - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.magnitude > StrikeRange) return;
            if (Vector3.Angle(transform.forward, toPlayer) > 65f) return;

            HitPlayer(Damage, 7f);
        }
    }

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
