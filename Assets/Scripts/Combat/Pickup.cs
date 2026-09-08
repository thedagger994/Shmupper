using UnityEngine;

namespace Shmupper
{
    public enum PickupKind { Ammo, Flask }

    /// Drops use a proximity check rather than trigger colliders. With a CharacterController
    /// player and dozens of loose objects a distance test is both cheaper and far more
    /// predictable - nothing is ever missed because it spawned inside geometry.
    public class Pickup : MonoBehaviour
    {
        public PickupKind Kind;

        Transform _player;
        PlayerHealth _health;
        WeaponSystem _weapons;
        float _life = 26f;
        float _spin;

        public static Pickup Spawn(PickupKind kind, Vector3 position, Transform player,
                                   PlayerHealth health, WeaponSystem weapons)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.position = position + Vector3.up * 0.6f;
            go.layer = Layers.Pickup;

            var p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p._player = player;
            p._health = health;
            p._weapons = weapons;
            p.BuildVisual();
            return p;
        }

        void BuildVisual()
        {
            if (Kind == PickupKind.Flask)
            {
                var glass = MatLib.Lit(Palette.ShrineGlow * 0.3f, Palette.ShrineGlow * 2.4f);
                var cap = MatLib.Lit(new Color(0.25f, 0.18f, 0.10f), default, 0.4f);

                Shapes.Ball(transform, Vector3.zero, 0.42f, glass);
                Shapes.Box(transform, Vector3.up * 0.28f, new Vector3(0.14f, 0.20f, 0.14f), cap);
                Shapes.Box(transform, Vector3.up * 0.40f, new Vector3(0.22f, 0.07f, 0.22f), cap);
            }
            else
            {
                var brass = MatLib.Lit(Palette.HudGold * 0.35f, Palette.HudGold * 2.2f);
                Shapes.Box(transform, Vector3.zero, new Vector3(0.46f, 0.26f, 0.30f), brass);
                Shapes.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.20f, 0f),
                    new Vector3(0.10f, 0.13f, 0.10f), brass);
            }

            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Kind == PickupKind.Flask ? Palette.ShrineGlow : Palette.HudGold;
            light.range = 6f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            _spin += Time.deltaTime * 110f;
            transform.rotation = Quaternion.Euler(0f, _spin, 0f);
            transform.position += Vector3.up * (Mathf.Sin(Time.time * 2.6f) * 0.004f);

            // Blink out the last three seconds so an expiring drop is never a surprise.
            if (_life < 3f)
            {
                bool visible = Mathf.Repeat(_life, 0.3f) > 0.12f;
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
            }

            if (_player == null) return;

            if (Vector3.Distance(transform.position, _player.position + Vector3.up * 0.8f) < 1.9f)
                Collect();
        }

        void Collect()
        {
            if (Kind == PickupKind.Flask)
            {
                if (_health != null && _health.Flasks >= _health.FlaskCapacity) return;
                _health?.AddFlask();
            }
            else
            {
                _weapons?.GiveSmartAmmo(1f);
            }

            Sfx.Play2D(SfxId.Pickup);
            Fx.Burst(transform.position, Kind == PickupKind.Flask ? Palette.ShrineGlow : Palette.HudGold, 1.2f, 0.25f);
            Destroy(gameObject);
        }
    }
}
