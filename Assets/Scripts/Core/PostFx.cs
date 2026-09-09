using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shmupper
{
    /// Reactive post-processing.
    ///
    /// The authored profile on the scene's Global Volume sets the castle's resting mood and is
    /// never touched at runtime - writing to it would dirty the asset in the editor and leave
    /// whatever the player's last hit looked like baked into the project. This component instead
    /// owns a second, higher priority volume whose profile is created in memory, and animates
    /// only that. Everything it does is additive on top of the authored look.
    ///
    /// The effects are feedback, not decoration: aberration and a red wash spike on damage, the
    /// vignette closes in and colour drains as health runs out, and a heal blooms mint. A player
    /// should be able to tell how close to death they are without reading the number.
    [DisallowMultipleComponent]
    public class PostFx : MonoBehaviour
    {
        static readonly Color HurtWash = new Color(1f, 0.42f, 0.38f);
        static readonly Color HealWash = new Color(0.55f, 1f, 0.82f);

        Volume _volume;
        VolumeProfile _profile;

        Vignette _vignette;
        ChromaticAberration _aberration;
        ColorAdjustments _color;
        FilmGrain _grain;
        LensDistortion _distortion;

        PlayerHealth _health;

        float _hurtPulse;
        float _healPulse;
        float _flashPulse;

        // Resting values the dynamic layer returns to. Kept here rather than read from the
        // authored profile so this component stays correct if that profile is retuned.
        const float BaseVignette = 0.30f;
        const float BaseAberration = 0.06f;
        const float BaseGrain = 0.32f;

        public static PostFx Create(Transform parent)
        {
            var go = new GameObject("PostFx");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<PostFx>();
        }

        void Awake()
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "ShmupperReactive";

            _vignette = _profile.Add<Vignette>(true);
            _vignette.color.value = new Color(0.03f, 0.02f, 0.05f);
            _vignette.intensity.value = BaseVignette;
            _vignette.smoothness.value = 0.45f;

            _aberration = _profile.Add<ChromaticAberration>(true);
            _aberration.intensity.value = BaseAberration;

            // Only the three parameters this component animates are marked as overridden. Taking
            // the whole component would blank the contrast and hue authored on the base profile,
            // since this volume sits above it.
            _color = _profile.Add<ColorAdjustments>(false);
            _color.colorFilter.overrideState = true;
            _color.saturation.overrideState = true;
            _color.postExposure.overrideState = true;
            _color.colorFilter.value = Color.white;
            _color.saturation.value = 0f;
            _color.postExposure.value = 0f;

            _grain = _profile.Add<FilmGrain>(true);
            _grain.type.value = FilmGrainLookup.Medium1;
            _grain.intensity.value = BaseGrain;
            _grain.response.value = 0.7f;

            _distortion = _profile.Add<LensDistortion>(true);
            _distortion.intensity.value = 0f;

            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            _volume.weight = 1f;
            _volume.sharedProfile = _profile;
        }

        public void Bind(PlayerHealth health)
        {
            if (_health != null)
            {
                _health.OnHurt -= HandleHurt;
                _health.OnHealed -= HandleHealed;
            }

            _health = health;

            if (_health != null)
            {
                _health.OnHurt += HandleHurt;
                _health.OnHealed += HandleHealed;
            }
        }

        void OnDestroy()
        {
            Bind(null);

            if (_profile != null) Destroy(_profile);
        }

        void HandleHurt(Vector3 _)
        {
            _hurtPulse = 1f;
        }

        void HandleHealed()
        {
            _healPulse = 1f;
        }

        /// A white bloom, used when a floor is cleared or the shrine opens.
        public void Flash(float strength = 1f)
        {
            _flashPulse = Mathf.Clamp01(strength);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            _hurtPulse = Mathf.MoveTowards(_hurtPulse, 0f, dt * 2.6f);
            _healPulse = Mathf.MoveTowards(_healPulse, 0f, dt * 1.8f);
            _flashPulse = Mathf.MoveTowards(_flashPulse, 0f, dt * 2.2f);

            // How close to death, 0 when healthy and 1 at the edge. Only the last third of the
            // health bar contributes, so the screen stays clean during ordinary fighting.
            float peril = 0f;
            if (_health != null && _health.MaxHealth > 0f)
                peril = 1f - Mathf.Clamp01(_health.Health / (_health.MaxHealth * 0.35f));

            float heartbeat = peril > 0.01f
                ? (Mathf.Sin(Time.time * Mathf.Lerp(3.5f, 7.5f, peril)) * 0.5f + 0.5f) * peril
                : 0f;

            _vignette.intensity.value = Mathf.Clamp01(
                BaseVignette + peril * 0.22f + heartbeat * 0.10f + _hurtPulse * 0.20f);

            _aberration.intensity.value = Mathf.Clamp01(
                BaseAberration + _hurtPulse * 0.55f + peril * 0.14f);

            _distortion.intensity.value = Mathf.Clamp(-_hurtPulse * 0.16f - peril * 0.05f, -0.5f, 0.5f);

            _grain.intensity.value = Mathf.Clamp01(BaseGrain + peril * 0.25f + _hurtPulse * 0.15f);

            // Damage drains colour and washes red; healing pushes the other way.
            _color.saturation.value = Mathf.Clamp(-peril * 34f - _hurtPulse * 22f + _healPulse * 12f, -100f, 100f);
            _color.postExposure.value = _flashPulse * 1.1f - _hurtPulse * 0.18f;

            Color filter = Color.white;
            if (_hurtPulse > 0.001f) filter = Color.Lerp(filter, HurtWash, _hurtPulse * 0.55f);
            if (_healPulse > 0.001f) filter = Color.Lerp(filter, HealWash, _healPulse * 0.40f);
            _color.colorFilter.value = filter;
        }
    }
}
