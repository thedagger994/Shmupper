using UnityEngine;

namespace Shmupper
{
    /// Wizards are the reason you cannot camp. They hold the far end of a hall, punish a still
    /// target with tracking bolts, and blink out of reach the moment you close the distance -
    /// so the answer is always to break line of sight and come at them from somewhere new.
    public class WizardEnemy : GroundEnemy
    {
        float _castCooldown;
        float _blinkCooldown;
        int _burstLeft;
        float _burstTimer;
        float _strafeSign = 1f;
        float _strafeTimer;

        const float PreferredRange = 13f;
        const float PanicRange = 6.5f;

        protected override void OnSpawned()
        {
            _castCooldown = Random.Range(0.6f, 1.8f);
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        protected override void Behave(float dt)
        {
            _castCooldown -= dt;
            _blinkCooldown -= dt;
            _strafeTimer -= dt;

            float dist = DistanceToPlayer;
            bool canSee = HasLineOfSight(transform.position + Vector3.up * 1.6f);

            if (_strafeTimer <= 0f)
            {
                _strafeTimer = Random.Range(1.1f, 2.3f);
                _strafeSign = -_strafeSign;
            }

            if (dist < PanicRange && _blinkCooldown <= 0f)
            {
                Blink();
            }
            else if (!canSee || dist > PreferredRange + 4f)
            {
                MoveAlong(ChaseDirection(), Speed, dt);
            }
            else
            {
                Vector3 away = (transform.position - PlayerPosition);
                away.y = 0f;
                away = away.normalized;

                Vector3 strafe = Vector3.Cross(Vector3.up, away) * _strafeSign;
                Vector3 hold = dist < PreferredRange ? away : -away;

                MoveAlong((hold * 0.6f + strafe).normalized, Speed, dt);
            }

            FaceMovement(PlayerPosition, dt, 8f);

            if (_burstLeft > 0)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    _burstTimer = 0.17f;
                    _burstLeft--;
                    FireBolt();
                }
                return;
            }

            if (_castCooldown <= 0f && canSee && dist < 34f)
            {
                _castCooldown = Random.Range(2.3f, 3.4f);
                _burstLeft = 3;
                _burstTimer = 0.12f;
            }
        }

        void FireBolt()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f + transform.forward * 0.7f;
            Vector3 dir = (PlayerAimPoint - origin).normalized;

            var bolt = Projectile.Spawn(origin, dir, false, 21f, Damage, 0.34f, Palette.Wizard, 0f, 2f, true, 5f);
            // A gentle homing rate: enough to punish standing still, slow enough that strafing
            // always beats it.
            bolt.SetHoming(Ctx.Player, 1.15f);

            Sfx.Play(SfxId.ShootFlux, origin, 0.7f, 0.7f);
        }

        void Blink()
        {
            _blinkCooldown = 4.2f;

            var map = Ctx.Map;
            if (map == null || map.SpawnCells.Count == 0) return;

            for (int attempt = 0; attempt < 14; attempt++)
            {
                var cell = map.SpawnCells[Random.Range(0, map.SpawnCells.Count)];
                Vector3 candidate = map.CellToWorld(cell, 0.2f);

                float d = Vector3.Distance(candidate, PlayerPosition);
                if (d < 10f || d > 24f) continue;

                Fx.Burst(transform.position + Vector3.up, Palette.Wizard, 2.6f, 0.35f);
                Sfx.Play(SfxId.ShootFlux, transform.position, 0.5f);

                Controller.enabled = false;
                transform.position = candidate;
                Controller.enabled = true;

                Fx.Burst(candidate + Vector3.up, Palette.Wizard, 2.6f, 0.35f);
                _castCooldown = Mathf.Min(_castCooldown, 0.6f);
                return;
            }
        }

        protected override void OnHurt(DamageInfo info)
        {
            // Taking a heavy hit makes a wizard bolt early, so a shotgun blast at range does not
            // simply delete one without a response.
            if (info.Amount > MaxHealth * 0.25f && _blinkCooldown > 1.5f) _blinkCooldown = 0.4f;
        }
    }
}
