using UnityEngine;

namespace Shmupper
{
    /// Builds and animates the first person weapon. The viewmodel is the single biggest source
    /// of "feel" in a shooter, so it gets real sway, bob and a spring-loaded recoil punch even
    /// though the guns themselves are twenty boxes each.
    public class WeaponView : MonoBehaviour
    {
        static readonly Vector3 RestPosition = new Vector3(0.30f, -0.26f, 0.52f);

        Transform _model;
        Transform _muzzle;
        Light _muzzleFlash;
        float _flashTimer;

        Vector3 _recoilOffset;
        Vector3 _recoilVelocity;
        float _bobPhase;

        public Transform Muzzle => _muzzle != null ? _muzzle : transform;

        public void ShowWeapon(WeaponId id)
        {
            if (_model != null)
            {
                // Destroy is deferred to the end of the frame, so the outgoing weapon is hidden
                // immediately - otherwise both models render together for one frame on a switch.
                _model.gameObject.SetActive(false);
                Destroy(_model.gameObject);
            }

            var root = new GameObject("ViewModel_" + id);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = RestPosition;
            _model = root.transform;

            var steel = MatLib.Lit(new Color(0.17f, 0.17f, 0.20f), default, 0.55f, 0.8f);
            var brass = MatLib.Lit(new Color(0.42f, 0.33f, 0.16f), default, 0.6f, 0.9f);
            var wood = MatLib.Lit(new Color(0.20f, 0.12f, 0.08f), default, 0.15f);

            var def = WeaponDef.Get(id);
            var glow = MatLib.Lit(def.Tint * 0.25f, def.Tint * 3.2f);

            switch (id)
            {
                case WeaponId.Spellslinger: BuildSpellslinger(root.transform, steel, brass, wood, glow); break;
                case WeaponId.Emberlance:   BuildEmberlance(root.transform, steel, brass, wood, glow); break;
                case WeaponId.Arcanoflux:   BuildArcanoflux(root.transform, steel, brass, glow); break;
                default:                    BuildRuneblaster(root.transform, steel, brass, glow); break;
            }

            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(root.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.02f, 0.62f);
            _muzzle = muzzleGo.transform;

            _muzzleFlash = muzzleGo.AddComponent<Light>();
            _muzzleFlash.type = LightType.Point;
            _muzzleFlash.color = def.Tint;
            _muzzleFlash.range = 12f;
            _muzzleFlash.intensity = 0f;
            _muzzleFlash.shadows = LightShadows.None;

            // The viewmodel must never clip into walls, so it renders on its own layer-free pass
            // by simply sitting very close to the camera and disabling shadow interaction.
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        static void BuildSpellslinger(Transform p, Material steel, Material brass, Material wood, Material glow)
        {
            Shapes.Box(p, new Vector3(0f, 0f, 0.12f), new Vector3(0.09f, 0.11f, 0.34f), steel);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.02f, 0.42f), new Vector3(0.055f, 0.20f, 0.055f), steel,
                Quaternion.Euler(90f, 0f, 0f));
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0f, 0.16f), new Vector3(0.10f, 0.05f, 0.10f), brass,
                Quaternion.Euler(90f, 0f, 0f));
            Shapes.Box(p, new Vector3(0f, -0.14f, -0.02f), new Vector3(0.07f, 0.20f, 0.10f), wood,
                Quaternion.Euler(-18f, 0f, 0f));
            Shapes.Box(p, new Vector3(0f, 0.055f, 0.16f), new Vector3(0.045f, 0.02f, 0.14f), glow);
            Shapes.Ball(p, new Vector3(0f, 0.02f, 0.60f), 0.05f, glow);
        }

        static void BuildEmberlance(Transform p, Material steel, Material brass, Material wood, Material glow)
        {
            Shapes.Box(p, new Vector3(0f, 0f, 0.10f), new Vector3(0.15f, 0.13f, 0.42f), steel);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(-0.05f, 0.02f, 0.44f), new Vector3(0.075f, 0.22f, 0.075f), steel,
                Quaternion.Euler(90f, 0f, 0f));
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0.05f, 0.02f, 0.44f), new Vector3(0.075f, 0.22f, 0.075f), steel,
                Quaternion.Euler(90f, 0f, 0f));
            // Flared brazier mouth: the cone points back toward the player so the wide opening
            // faces down the barrel.
            Shapes.Cone(p, new Vector3(0f, 0.02f, 0.68f), 0.13f, 0.16f, glow, 8, Quaternion.Euler(-90f, 0f, 0f));
            Shapes.Box(p, new Vector3(0f, -0.13f, -0.10f), new Vector3(0.09f, 0.18f, 0.14f), wood,
                Quaternion.Euler(-22f, 0f, 0f));
            Shapes.Box(p, new Vector3(0f, 0.08f, 0.10f), new Vector3(0.06f, 0.03f, 0.22f), brass);
            Shapes.Ball(p, new Vector3(0f, 0.085f, 0.22f), 0.06f, glow);
        }

        static void BuildArcanoflux(Transform p, Material steel, Material brass, Material glow)
        {
            Shapes.Box(p, new Vector3(0f, 0f, 0.08f), new Vector3(0.10f, 0.14f, 0.38f), steel);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.03f, 0.40f), new Vector3(0.045f, 0.20f, 0.045f), steel,
                Quaternion.Euler(90f, 0f, 0f));

            for (int i = 0; i < 4; i++)
            {
                Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.03f, 0.28f + i * 0.10f),
                    new Vector3(0.10f, 0.012f, 0.10f), glow, Quaternion.Euler(90f, 0f, 0f));
            }

            Shapes.Box(p, new Vector3(0f, -0.16f, 0.02f), new Vector3(0.07f, 0.22f, 0.09f), steel);
            Shapes.Box(p, new Vector3(0f, -0.09f, 0.16f), new Vector3(0.055f, 0.16f, 0.07f), brass);
            Shapes.Ball(p, new Vector3(0f, 0.12f, 0.06f), 0.09f, glow);
        }

        static void BuildRuneblaster(Transform p, Material steel, Material brass, Material glow)
        {
            Shapes.Box(p, new Vector3(0f, 0f, 0.06f), new Vector3(0.17f, 0.17f, 0.46f), steel);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.02f, 0.46f), new Vector3(0.15f, 0.16f, 0.15f), steel,
                Quaternion.Euler(90f, 0f, 0f));
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.02f, 0.63f), new Vector3(0.19f, 0.03f, 0.19f), glow,
                Quaternion.Euler(90f, 0f, 0f));

            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Shapes.Box(p, new Vector3(Mathf.Cos(a) * 0.13f, 0.02f + Mathf.Sin(a) * 0.13f, 0.34f),
                    new Vector3(0.03f, 0.03f, 0.12f), glow);
            }

            Shapes.Box(p, new Vector3(0f, -0.17f, -0.02f), new Vector3(0.08f, 0.22f, 0.11f), brass,
                Quaternion.Euler(-15f, 0f, 0f));
        }

        public void Kick(float amount)
        {
            _recoilVelocity += new Vector3(
                Random.Range(-0.02f, 0.02f) * amount,
                0.035f * amount,
                -0.09f * amount);

            _flashTimer = 0.06f;
            if (_muzzleFlash != null) _muzzleFlash.intensity = 5f;
        }

        public void Tick(float speed01, bool grounded)
        {
            if (_model == null) return;

            // Critically damped spring back to rest - snappier than a lerp and it never
            // overshoots into the camera.
            _recoilVelocity -= _recoilOffset * (52f * Time.deltaTime);
            _recoilVelocity *= Mathf.Exp(-11f * Time.deltaTime);
            _recoilOffset += _recoilVelocity * Time.deltaTime;

            _bobPhase += Time.deltaTime * (grounded ? speed01 * 11f : 2f);
            float bobX = Mathf.Cos(_bobPhase) * 0.022f * speed01;
            float bobY = Mathf.Abs(Mathf.Sin(_bobPhase)) * -0.020f * speed01;

            _model.localPosition = RestPosition + _recoilOffset + new Vector3(bobX, bobY, 0f);
            _model.localRotation = Quaternion.Euler(
                _recoilOffset.z * 90f,
                bobX * 40f,
                bobX * 60f);

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_muzzleFlash != null)
                    _muzzleFlash.intensity = Mathf.Max(0f, _muzzleFlash.intensity - Time.deltaTime * 90f);
            }
        }
    }
}
