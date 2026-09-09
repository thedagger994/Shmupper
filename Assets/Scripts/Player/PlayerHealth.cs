using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmupper
{
    /// Player health plus the healing flask economy. Flasks are deliberately a manual button
    /// press rather than automatic regeneration: deciding when to spend one, mid fight, with a
    /// dragon still circling, is the only resource decision the player makes outside the shrine.
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public event Action<float, float> OnHealthChanged;
        public event Action<int, int> OnFlasksChanged;
        public event Action<Vector3> OnHurt;
        public event Action OnHealed;
        public event Action OnDied;

        RunState _run;
        PlayerController _controller;

        float _health;
        int _flasks;
        float _invulnerableUntil;

        public bool IsAlive => _health > 0f;
        public Transform Transform => transform;
        public float Health => _health;
        public float MaxHealth => _run != null ? _run.MaxHealth : 100f;
        public int Flasks => _flasks;
        public int FlaskCapacity => _run != null ? _run.FlaskCapacity : 1;
        public bool InputEnabled = true;

        public void Init(RunState run, PlayerController controller)
        {
            _run = run;
            _controller = controller;
            _health = run.MaxHealth;
            _flasks = run.FlaskCapacity;
            Broadcast();
        }

        public void RefillForFloor()
        {
            _health = Mathf.Min(MaxHealth, _health + MaxHealth * 0.35f);
            _flasks = Mathf.Max(_flasks, Mathf.Min(FlaskCapacity, _flasks + 1));
            Broadcast();
        }

        public void FullRestore()
        {
            _health = MaxHealth;
            _flasks = FlaskCapacity;
            Broadcast();
        }

        /// Called when Vitality is purchased: the new maximum arrives already filled, which is
        /// what makes the upgrade feel like a reward instead of a bigger empty bar.
        public void OnMaxHealthRaised()
        {
            _health = MaxHealth;
            Broadcast();
        }

        public void OnFlaskCapacityRaised()
        {
            _flasks = FlaskCapacity;
            Broadcast();
        }

        public void AddFlask(int amount = 1)
        {
            if (_flasks >= FlaskCapacity) return;

            _flasks = Mathf.Min(FlaskCapacity, _flasks + amount);
            Broadcast();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            _health = Mathf.Min(MaxHealth, _health + amount);
            OnHealed?.Invoke();
            Broadcast();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || Time.time < _invulnerableUntil) return;

            float amount = info.Amount * (_run != null ? _run.DamageTakenMul : 1f);
            _health -= amount;
            _invulnerableUntil = Time.time + 0.12f;

            if (_run != null) _run.TookDamageOnThisFloor = true;

            // Trauma scales with the bite the hit took out of the player rather than its raw
            // number, so a 20 point hit on a nearly dead Gunmage rattles the screen far harder
            // than the same hit at full health.
            float severity = Mathf.Clamp01(amount / Mathf.Max(1f, MaxHealth * 0.28f));
            _controller?.Shake(Mathf.Lerp(0.22f, 0.85f, severity), Mathf.Lerp(0.30f, 0.55f, severity));

            // Punch the view away from whatever hit us, so damage has a direction.
            Vector3 from = info.Point - (transform.position + Vector3.up * 0.9f);
            if (from.sqrMagnitude > 0.01f)
                _controller?.Punch(-from.normalized, Mathf.Lerp(0.10f, 0.34f, severity));

            if (info.Push.sqrMagnitude > 0.01f) _controller?.AddImpulse(info.Push * 0.6f);

            Sfx.Play2D(SfxId.PlayerHurt, 1f, Mathf.Clamp01(amount / 30f) * 0.8f + 0.2f);
            OnHurt?.Invoke(info.Point);
            Broadcast();

            if (_health <= 0f)
            {
                _health = 0f;
                OnDied?.Invoke();
            }
        }

        void Update()
        {
            if (!InputEnabled || !IsAlive) return;

            var kb = Keyboard.current;
            bool wants = (kb != null && (kb.fKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame));

            var pad = Gamepad.current;
            if (!wants && pad != null) wants = pad.buttonWest.wasPressedThisFrame;

            if (wants) UseFlask();
        }

        public void UseFlask()
        {
            if (_flasks <= 0 || !IsAlive)
            {
                Sfx.Play2D(SfxId.UiDeny, 1f, 0.5f);
                return;
            }

            if (_health >= MaxHealth - 0.01f)
            {
                Sfx.Play2D(SfxId.UiDeny, 1f, 0.5f);
                return;
            }

            _flasks--;
            _health = Mathf.Min(MaxHealth, _health + MaxHealth * 0.45f);

            OnHealed?.Invoke();
            Sfx.Play2D(SfxId.Heal);
            Fx.Burst(transform.position + Vector3.up * 0.8f, Palette.ShrineGlow, 2.2f, 0.4f);
            Broadcast();
        }

        void Broadcast()
        {
            OnHealthChanged?.Invoke(_health, MaxHealth);
            OnFlasksChanged?.Invoke(_flasks, FlaskCapacity);
        }
    }
}
