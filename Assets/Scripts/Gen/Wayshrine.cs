using System;
using UnityEngine;

namespace Shmupper
{
    /// The end-of-floor landmark. It is deliberately loud - a pillar of light visible across the
    /// castle and a compass marker - because once the floor is clear the player should never
    /// have to wonder where to go next.
    public class Wayshrine : MonoBehaviour
    {
        public event Action OnPlayerArrived;

        Transform _player;
        Transform _ring;
        Light _light;
        bool _armed;
        bool _consumed;
        float _pulse;

        public bool IsArmed => _armed;

        public static Wayshrine Create(Vector3 position, Transform player)
        {
            var go = new GameObject("Wayshrine");
            go.transform.position = position;

            var shrine = go.AddComponent<Wayshrine>();
            shrine._player = player;
            shrine.Build();
            shrine.SetArmed(false);

            // The shrine counts as level geometry: bolts should hit it and enemies should not be
            // able to see the player through it.
            Layers.Apply(go, Layers.Level);
            return shrine;
        }

        void Build()
        {
            var stone = MatLib.Lit(new Color(0.30f, 0.31f, 0.36f), default, 0.2f);
            var glow = MatLib.Lit(Palette.ShrineGlow * 0.3f, Palette.ShrineGlow * 3f);

            Shapes.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.1f, 0f),
                new Vector3(4.2f, 0.2f, 4.2f), stone, null, true);
            Shapes.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.32f, 0f),
                new Vector3(3.2f, 0.2f, 3.2f), stone, null, true);

            Shapes.Box(transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.9f, 2.2f, 0.9f), stone, null, true);
            Shapes.Cone(transform, new Vector3(0f, 2.4f, 0f), 0.62f, 1.5f, stone, 6);

            Shapes.Box(transform, new Vector3(0f, 1.3f, 0.47f), new Vector3(0.4f, 1.4f, 0.06f), glow);
            Shapes.Box(transform, new Vector3(0f, 1.3f, -0.47f), new Vector3(0.4f, 1.4f, 0.06f), glow);
            Shapes.Box(transform, new Vector3(0.47f, 1.3f, 0f), new Vector3(0.06f, 1.4f, 0.4f), glow);
            Shapes.Box(transform, new Vector3(-0.47f, 1.3f, 0f), new Vector3(0.06f, 1.4f, 0.4f), glow);

            var ring = new GameObject("Runes");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Shapes.Box(ring.transform, new Vector3(Mathf.Cos(a) * 1.6f, 0f, Mathf.Sin(a) * 1.6f),
                    new Vector3(0.16f, 0.42f, 0.16f), glow, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }
            _ring = ring.transform;

            // A column of light filling the room from floor to ceiling. It stops at the ceiling
            // rather than passing through it - the compass is what finds the shrine from across
            // the floor, and a beam poking through solid stone would just read as a bug.
            Shapes.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 3f, 0f),
                new Vector3(1.1f, 3f, 1.1f), MatLib.Lit(Palette.ShrineGlow * 0.12f, Palette.ShrineGlow * 1.1f));

            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = Palette.ShrineGlow;
            _light.range = 22f;
            _light.intensity = 2.4f;
            _light.shadows = LightShadows.None;
        }

        public void SetArmed(bool armed)
        {
            _armed = armed;
            _consumed = false;

            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            if (_light != null) _light.intensity = armed ? 3.4f : 1.1f;

            if (armed)
            {
                Fx.Burst(transform.position + Vector3.up * 2f, Palette.ShrineGlow, 6f, 0.6f);
                Sfx.Play(SfxId.Shrine, transform.position);
            }
        }

        void Update()
        {
            _pulse += Time.deltaTime;

            if (_ring != null)
            {
                _ring.localRotation = Quaternion.Euler(0f, _pulse * (_armed ? 55f : 16f), 0f);
                float lift = Mathf.Sin(_pulse * (_armed ? 2.4f : 1.1f)) * 0.16f;
                _ring.localPosition = new Vector3(0f, 2.1f + lift, 0f);
            }

            if (_light != null)
                _light.intensity = (_armed ? 3.4f : 1.1f) * (0.85f + Mathf.Sin(_pulse * 3f) * 0.15f);

            if (!_armed || _consumed || _player == null) return;

            if (Vector3.Distance(transform.position, _player.position) < 3.4f)
            {
                _consumed = true;
                OnPlayerArrived?.Invoke();
            }
        }
    }
}
