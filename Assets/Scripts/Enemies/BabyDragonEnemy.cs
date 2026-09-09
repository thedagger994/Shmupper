using UnityEngine;

namespace Shmupper
{
    /// Baby dragons own the vertical space. They circle above the fight, spit fire down at the
    /// player, and periodically commit to a dive - the only enemy that forces the player to look
    /// up in a game otherwise fought at eye level.
    public class BabyDragonEnemy : FlyingEnemy
    {
        enum State { Circle, DiveWindUp, Dive, Climb }

        State _state = State.Circle;
        float _timer;
        float _spitCooldown;
        float _phase;
        Vector3 _diveTarget;

        protected override void OnSpawned()
        {
            _phase = Random.value * Mathf.PI * 2f;
            _spitCooldown = Random.Range(0.8f, 2.2f);
            DesiredAltitude = Random.Range(3.6f, 4.8f);
            Velocity = Vector3.zero;
        }

        protected override void Behave(float dt)
        {
            _timer -= dt;
            _spitCooldown -= dt;

            switch (_state)
            {
                case State.Circle:
                {
                    Vector3 orbit = OrbitPoint(9.5f, DesiredAltitude, _phase, 0.65f);
                    Steer(orbit, Speed, dt);
                    FaceTarget(PlayerAimPoint, dt);

                    if (_spitCooldown <= 0f && HasLineOfSight(transform.position) && DistanceToPlayer < 26f)
                    {
                        _spitCooldown = Random.Range(1.6f, 2.6f);
                        Spit();
                    }

                    if (_timer <= 0f && DistanceToPlayer < 16f && HasLineOfSight(transform.position))
                    {
                        _state = State.DiveWindUp;
                        _timer = 0.55f;
                        _diveTarget = PlayerAimPoint;
                        Fx.Ring(new Vector3(PlayerPosition.x, PlayerPosition.y + 0.05f, PlayerPosition.z),
                            2.6f, Palette.BabyDragon, 0.55f);
                    }
                    break;
                }

                case State.DiveWindUp:
                    Steer(transform.position + Vector3.up * 1.2f, Speed * 0.5f, dt, 5f);
                    FaceTarget(_diveTarget, dt, 10f);
                    if (_timer <= 0f) { _state = State.Dive; _timer = 1.1f; }
                    break;

                case State.Dive:
                {
                    Steer(_diveTarget, Speed * 2.3f, dt, 6f);

                    if (Vector3.Distance(transform.position, PlayerAimPoint) < 1.9f)
                    {
                        HitPlayer(Damage, 6f);
                        Sfx.Play(SfxId.ShootShotgun, transform.position, 1.2f, 0.6f);
                        _state = State.Climb;
                        _timer = 1.0f;
                    }
                    else if (_timer <= 0f)
                    {
                        _state = State.Climb;
                        _timer = 1.0f;
                    }
                    break;
                }

                default:
                    Steer(OrbitPoint(11f, DesiredAltitude + 1.2f, _phase, 0.65f), Speed * 1.4f, dt);
                    FaceTarget(PlayerAimPoint, dt);
                    if (_timer <= 0f) { _state = State.Circle; _timer = Random.Range(3.5f, 6f); }
                    break;
            }
        }

        void Spit()
        {
            Vector3 origin = transform.position + transform.forward * 0.9f;
            Vector3 dir = (PlayerAimPoint - origin).normalized;

            Projectile.Spawn(origin, dir, false, 26f, Damage * 0.8f, 0.4f, Palette.BabyDragon, 2.4f, 3f, false, 4.5f);
            Sfx.Play(SfxId.ShootRocket, origin, 1.35f, 0.55f);
        }
    }
}
