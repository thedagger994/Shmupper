using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmupper
{
    /// Firing, switching and ammo. Weapons unlock by depth rather than by pickup so the player
    /// always knows what they own, and every gun holds to fire - a shmup should never make you
    /// mash a button.
    public class WeaponSystem : MonoBehaviour
    {
        public event Action<WeaponRuntime> OnWeaponChanged;
        public event Action<string> OnAnnounce;

        WeaponRuntime[] _weapons;
        int _index;
        Camera _camera;
        PlayerController _player;
        WeaponView _view;
        RunState _run;

        public WeaponRuntime Current => _weapons[_index];
        public WeaponRuntime[] Weapons => _weapons;
        public bool InputEnabled = true;

        public void Init(Camera cam, PlayerController player, RunState run)
        {
            _camera = cam;
            _player = player;
            _run = run;

            _view = cam.GetComponent<WeaponView>();
            if (_view == null) _view = cam.gameObject.AddComponent<WeaponView>();

            ResetForNewRun(run);
        }

        /// Rebuilds the arsenal for a fresh run without touching the viewmodel component or any
        /// event subscriptions, both of which outlive individual runs.
        public void ResetForNewRun(RunState run)
        {
            _run = run;

            _weapons = new WeaponRuntime[WeaponDef.All.Length];
            for (int i = 0; i < WeaponDef.All.Length; i++)
                _weapons[i] = new WeaponRuntime(WeaponDef.All[i]);

            _index = 0;
            _view.ShowWeapon(Current.Def.Id);
            OnWeaponChanged?.Invoke(Current);
        }

        /// Called when the player reaches a new depth. Newly unlocked weapons arrive loaded so
        /// the player can try them immediately instead of hunting for ammo first.
        public void UnlockForFloor(int floor)
        {
            foreach (var w in _weapons)
            {
                if (w.Unlocked || w.Def.UnlockFloor > floor) continue;

                w.Unlocked = true;
                w.Refill(_run);
                OnAnnounce?.Invoke("RECOVERED: " + w.Def.Name);
                Sfx.Play2D(SfxId.UiSelect);
            }
        }

        public void RefillAll()
        {
            foreach (var w in _weapons)
                if (w.Unlocked) w.Refill(_run);
        }

        public void GiveAmmo(WeaponId id, int amount)
        {
            foreach (var w in _weapons)
                if (w.Def.Id == id) w.AddAmmo(_run, amount);
        }

        /// Ammo drops feed whichever unlocked weapon is emptiest relative to its capacity, so
        /// pickups never feel wasted.
        public void GiveSmartAmmo(float scale)
        {
            WeaponRuntime best = null;
            float worstRatio = float.MaxValue;

            foreach (var w in _weapons)
            {
                if (!w.Unlocked || w.Def.InfiniteAmmo) continue;

                float ratio = w.Ammo / (float)Mathf.Max(1, w.MaxAmmo(_run));
                if (ratio < worstRatio) { worstRatio = ratio; best = w; }
            }

            if (best == null) return;
            best.AddAmmo(_run, Mathf.Max(1, Mathf.RoundToInt(best.Def.AmmoPerPickup * scale)));
        }

        void Update()
        {
            if (_weapons == null) return;

            _view.Tick(_player != null ? _player.SpeedFraction : 0f, _player == null || _player.IsGrounded);

            if (!InputEnabled) return;

            HandleSwitching();

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) TryFire();
        }

        void HandleSwitching()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Select(0);
                if (kb.digit2Key.wasPressedThisFrame) Select(1);
                if (kb.digit3Key.wasPressedThisFrame) Select(2);
                if (kb.digit4Key.wasPressedThisFrame) Select(3);
                if (kb.qKey.wasPressedThisFrame) Cycle(-1);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) Cycle(1);
                else if (scroll < -0.1f) Cycle(-1);
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _weapons.Length) return;
            if (index == _index || !_weapons[index].Unlocked) return;

            _index = index;
            _view.ShowWeapon(Current.Def.Id);
            Sfx.Play2D(SfxId.UiMove, 1.3f, 0.6f);
            OnWeaponChanged?.Invoke(Current);
        }

        void Cycle(int dir)
        {
            for (int step = 1; step <= _weapons.Length; step++)
            {
                int i = ((_index + dir * step) % _weapons.Length + _weapons.Length) % _weapons.Length;
                if (_weapons[i].Unlocked) { Select(i); return; }
            }
        }

        void TryFire()
        {
            var w = Current;
            if (Time.time < w.NextShotTime) return;

            if (!w.HasAmmo)
            {
                w.NextShotTime = Time.time + 0.25f;
                Sfx.Play2D(SfxId.DryFire);
                FallBackToLoadedWeapon();
                return;
            }

            var def = w.Def;
            w.NextShotTime = Time.time + 1f / Mathf.Max(0.05f, def.ShotsPerSecond * _run.FireRateMul);
            if (!def.InfiniteAmmo) w.Ammo--;

            float damage = def.Damage * _run.DamageMul;
            Vector3 origin = _camera.transform.position;
            Vector3 muzzle = _view.Muzzle.position;

            if (def.Hitscan)
            {
                for (int i = 0; i < def.Pellets; i++)
                    FireHitscanPellet(origin, muzzle, def, damage);
            }
            else
            {
                Projectile.Spawn(muzzle + _camera.transform.forward * 0.4f, _camera.transform.forward, true,
                    def.ProjectileSpeed, damage, 0.42f, def.Tint, def.SplashRadius, def.Knockback, true);
            }

            _view.Kick(def.RecoilKick);
            _player?.AddRecoil(def.RecoilKick * 0.32f);

            // Only the heavy weapons punch the view. Doing it on the Arcanoflux at eleven shots a
            // second would leave the field of view permanently pumping.
            if (def.RecoilKick >= 3f)
            {
                _player?.AddFovKick(def.RecoilKick * 0.075f);
                _player?.Shake(def.RecoilKick * 0.035f, 0.18f);
            }

            Sfx.Play(def.Sound, muzzle);
        }

        void FireHitscanPellet(Vector3 origin, Vector3 muzzle, WeaponDef def, float damage)
        {
            Vector3 dir = SpreadDirection(_camera.transform.forward, def.SpreadDegrees);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, def.Range, Layers.ShotMask, QueryTriggerInteraction.Ignore))
            {
                Fx.Tracer(muzzle, hit.point, def.Tint, def.Pellets > 1 ? 0.035f : 0.055f);

                var target = DamageUtil.Find(hit.collider);
                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(new DamageInfo
                    {
                        Amount = damage,
                        Point = hit.point,
                        Normal = hit.normal,
                        Push = dir * def.Knockback,
                        FromPlayer = true
                    });
                }
                else
                {
                    Fx.Impact(hit.point, hit.normal, new Color(0.9f, 0.85f, 0.75f), 3, 0.7f);
                    Sfx.Play(SfxId.Impact, hit.point, 1f, 0.5f);
                }
            }
            else
            {
                Fx.Tracer(muzzle, origin + dir * def.Range, def.Tint, 0.03f);
            }
        }

        static Vector3 SpreadDirection(Vector3 forward, float degrees)
        {
            if (degrees <= 0.001f) return forward;

            Vector2 disc = UnityEngine.Random.insideUnitCircle * degrees;
            return Quaternion.AngleAxis(disc.x, Vector3.up) *
                   Quaternion.AngleAxis(disc.y, Vector3.Cross(Vector3.up, forward).normalized) * forward;
        }

        void FallBackToLoadedWeapon()
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i].Unlocked && _weapons[i].HasAmmo) { Select(i); return; }
            }
        }
    }
}
