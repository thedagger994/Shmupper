using UnityEngine;

namespace Shmupper
{
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
}
