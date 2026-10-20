using UnityEngine;

namespace Shmupper
{
    /// Animates one wall sconce.
    ///
    /// The flame used to only pulse in place, which made every torch look pinned to the wall. It
    /// now sways on its bracket, the flame leans with the sway and bobs independently of it, and
    /// embers drift off it. Each torch carries its own noise offset so a corridor of sconces never
    /// moves in unison - matching phase is the single thing that makes procedural animation read
    /// as fake.
    ///
    /// Embers are emitted into the shared particle pool on a stagger rather than from a system per
    /// torch, and only while the player is close enough to see them. A floor holds up to a
    /// hundred and fifty of these, so neither the emission nor the animation can afford to be
    /// expensive.
    public class TorchFlicker : MonoBehaviour
    {
        const float EmberRange = 34f;
        const float CullRange = 48f;

        Light _light;
        Transform _flame;
        Transform _mount;
        float _baseIntensity;
        float _seed;
        float _emberTimer;
        Vector3 _flameRest;
        Color _emberColor = Palette.TorchLight;

        public void Setup(Light light, Transform flame, float baseIntensity, Transform mount = null)
        {
            _light = light;
            _flame = flame;
            _mount = mount;
            _baseIntensity = baseIntensity;
            _seed = Random.value * 100f;
            _emberTimer = Random.Range(0f, 0.4f);

            if (_flame != null) _flameRest = _flame.localPosition;
            if (light != null) _emberColor = light.color;
        }

        /// Set when the sconce burns arcane blue rather than orange, so its embers match.
        public void SetEmberColor(Color color) => _emberColor = color;

        void Update()
        {
            // One distance check gates everything. Torches across the floor cost a subtraction.
            var cam = Camera.main;
            float distanceSqr = cam != null
                ? (cam.transform.position - transform.position).sqrMagnitude
                : 0f;

            if (cam != null && distanceSqr > CullRange * CullRange) return;

            float dt = Time.deltaTime;
            float t = Time.time;

            float n = Mathf.PerlinNoise(_seed, t * 3.1f);
            float sway = Mathf.PerlinNoise(_seed + 11f, t * 0.85f) - 0.5f;
            float pulse = 0.78f + n * 0.44f;

            if (_light != null) _light.intensity = _baseIntensity * pulse;

            // The bracket leans slowly, as if the flame's draught were pushing it.
            if (_mount != null)
            {
                _mount.localRotation = Quaternion.Euler(
                    sway * 4.5f,
                    0f,
                    (Mathf.PerlinNoise(_seed + 29f, t * 0.7f) - 0.5f) * 6f);
            }

            if (_flame != null)
            {
                float s = 0.5f + n * 0.16f;
                _flame.localScale = new Vector3(s, s * (1.05f + n * 0.25f), s);

                // Bob and lean on a different frequency from the bracket, so the two motions
                // never lock into a single visible rhythm.
                _flame.localPosition = _flameRest + new Vector3(
                    sway * 0.07f,
                    Mathf.Sin(t * 2.4f + _seed) * 0.035f + n * 0.03f,
                    (Mathf.PerlinNoise(_seed + 53f, t * 1.3f) - 0.5f) * 0.07f);
            }

            if (cam == null || distanceSqr > EmberRange * EmberRange) return;

            _emberTimer -= dt;
            if (_emberTimer > 0f) return;

            // Staggered randomly rather than on a fixed interval, so a row of torches does not
            // release a visible pulse of embers together.
            _emberTimer = Random.Range(0.14f, 0.42f);

            Vector3 origin = _flame != null ? _flame.position : transform.position;
            FxParticles.Ember(origin, _emberColor * Mathf.Lerp(0.7f, 1.3f, n));
        }
    }
}
