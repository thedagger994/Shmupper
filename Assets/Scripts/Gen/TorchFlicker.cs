using UnityEngine;

namespace Shmupper
{
    /// Cheap per-torch animation. Each torch gets its own noise offset so a corridor of sconces
    /// never pulses in unison, which is the thing that makes procedural lighting look fake.
    public class TorchFlicker : MonoBehaviour
    {
        Light _light;
        Transform _flame;
        float _baseIntensity;
        float _seed;

        public void Setup(Light light, Transform flame, float baseIntensity)
        {
            _light = light;
            _flame = flame;
            _baseIntensity = baseIntensity;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 3.1f);
            float pulse = 0.78f + n * 0.44f;

            if (_light != null) _light.intensity = _baseIntensity * pulse;

            if (_flame != null)
            {
                float s = 0.5f + n * 0.16f;
                _flame.localScale = new Vector3(s, s * (1.05f + n * 0.25f), s);
            }
        }
    }
}
