using UnityEngine;

namespace Shmupper
{
    /// The bestiary, built from boxes, spheres and cones.
    ///
    /// Every silhouette is designed to be readable in one glance at speed and in the dark: one
    /// signature colour, one signature shape, one glowing tell. A hollow knight is a floating
    /// helm over an empty gorget; a wizard is a cone with an orbiting orb; a baby dragon is the
    /// only thing in the castle with wings and a lit throat.
    public static class EnemyBuilder
    {
        public static Transform Build(EnemyKind kind, Transform parent)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);

            switch (kind)
            {
                case EnemyKind.Imp:          BuildImp(body.transform); break;
                case EnemyKind.HollowKnight: BuildHollowKnight(body.transform); break;
                case EnemyKind.Wizard:       BuildWizard(body.transform); break;
                case EnemyKind.BabyDragon:   BuildBabyDragon(body.transform); break;
                case EnemyKind.Gargoyle:     BuildGargoyle(body.transform); break;
                default:                     BuildLich(body.transform); break;
            }

            return body.transform;
        }

        static Material Flesh(Color c) => MatLib.Lit(c * 0.55f, default, 0.2f);
        static Material Dark(float v = 0.11f) => MatLib.Lit(new Color(v, v, v * 1.15f), default, 0.25f);
        static Material Glow(Color c, float strength = 3f) => MatLib.Lit(c * 0.3f, c * strength);
        static Material Stone(Color c) => MatLib.Lit(c * 0.4f, default, 0.08f);
        static Material Steel(Color c) => MatLib.Lit(c * 0.35f, default, 0.6f, 0.75f);

        // ------------------------------------------------------------------- imp

        static void BuildImp(Transform p)
        {
            var skin = Flesh(Palette.Imp);
            var eye = Glow(Palette.Imp, 4f);

            Shapes.Ball(p, new Vector3(0f, 0.55f, 0f), 0.85f, skin);
            Shapes.Cone(p, new Vector3(-0.2f, 0.85f, 0f), 0.09f, 0.38f, skin, 5, Quaternion.Euler(0f, 0f, 18f));
            Shapes.Cone(p, new Vector3(0.2f, 0.85f, 0f), 0.09f, 0.38f, skin, 5, Quaternion.Euler(0f, 0f, -18f));

            Shapes.Box(p, new Vector3(0f, 0.62f, 0.36f), new Vector3(0.42f, 0.10f, 0.06f), eye);

            Shapes.Box(p, new Vector3(-0.45f, 0.52f, 0.05f), new Vector3(0.12f, 0.34f, 0.12f), skin,
                Quaternion.Euler(0f, 0f, 24f));
            Shapes.Box(p, new Vector3(0.45f, 0.52f, 0.05f), new Vector3(0.12f, 0.34f, 0.12f), skin,
                Quaternion.Euler(0f, 0f, -24f));

            Shapes.Box(p, new Vector3(-0.18f, 0.16f, 0f), new Vector3(0.16f, 0.34f, 0.16f), skin);
            Shapes.Box(p, new Vector3(0.18f, 0.16f, 0f), new Vector3(0.16f, 0.34f, 0.16f), skin);

            Bob(p.gameObject, 0.05f, 7f);
        }

        // ---------------------------------------------------------- hollow knight

        static void BuildHollowKnight(Transform p)
        {
            var plate = Steel(Palette.HollowKnight);
            var dark = Dark(0.07f);
            var eye = Glow(Palette.HollowKnight, 4.5f);

            Shapes.Box(p, new Vector3(0f, 1.05f, 0f), new Vector3(0.78f, 0.95f, 0.48f), plate);
            Shapes.Box(p, new Vector3(0f, 1.52f, 0f), new Vector3(0.60f, 0.14f, 0.40f), dark);

            // The gorget is empty. Leaving a visible gap under the helm is the entire read on
            // this enemy - armour walking with nobody inside it.
            var helm = new GameObject("Helm");
            helm.transform.SetParent(p, false);
            helm.transform.localPosition = new Vector3(0f, 1.86f, 0f);

            Shapes.Box(helm.transform, Vector3.zero, new Vector3(0.46f, 0.42f, 0.46f), plate);
            Shapes.Cone(helm.transform, new Vector3(0f, 0.2f, 0f), 0.16f, 0.42f, plate, 4);
            Shapes.Box(helm.transform, new Vector3(0f, -0.02f, 0.24f), new Vector3(0.30f, 0.07f, 0.06f), eye);
            Bob(helm, 0.045f, 2.4f);

            Shapes.Box(p, new Vector3(-0.52f, 1.42f, 0f), new Vector3(0.34f, 0.22f, 0.44f), plate,
                Quaternion.Euler(0f, 0f, 20f));
            Shapes.Box(p, new Vector3(0.52f, 1.42f, 0f), new Vector3(0.34f, 0.22f, 0.44f), plate,
                Quaternion.Euler(0f, 0f, -20f));

            Shapes.Pill(p, new Vector3(-0.55f, 1.02f, 0.05f), new Vector3(0.20f, 0.34f, 0.20f), plate);
            Shapes.Pill(p, new Vector3(0.55f, 1.02f, 0.05f), new Vector3(0.20f, 0.34f, 0.20f), plate);

            var sword = new GameObject("Sword");
            sword.transform.SetParent(p, false);
            sword.transform.localPosition = new Vector3(0.62f, 0.95f, 0.42f);
            sword.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            Shapes.Box(sword.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.09f, 0.03f, 1.25f), plate);
            Shapes.Box(sword.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.02f, 0.05f, 1.20f), eye);
            Shapes.Box(sword.transform, Vector3.zero, new Vector3(0.30f, 0.07f, 0.08f), dark);

            Shapes.Box(p, new Vector3(-0.22f, 0.32f, 0f), new Vector3(0.24f, 0.66f, 0.26f), plate);
            Shapes.Box(p, new Vector3(0.22f, 0.32f, 0f), new Vector3(0.24f, 0.66f, 0.26f), plate);
        }

        // ---------------------------------------------------------------- wizard

        static void BuildWizard(Transform p)
        {
            var robe = Flesh(Palette.Wizard);
            var trim = Glow(Palette.Wizard, 2.2f);
            var dark = Dark(0.06f);
            var eye = Glow(Palette.Wizard, 5f);

            Shapes.Cone(p, new Vector3(0f, 0.05f, 0f), 0.62f, 1.55f, robe, 8);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 0.72f, 0f), new Vector3(0.62f, 0.04f, 0.62f), trim);

            Shapes.Ball(p, new Vector3(0f, 1.68f, 0f), 0.42f, dark);
            Shapes.Box(p, new Vector3(0f, 1.70f, 0.19f), new Vector3(0.24f, 0.06f, 0.06f), eye);

            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 1.88f, 0f), new Vector3(0.86f, 0.03f, 0.86f), robe);
            Shapes.Cone(p, new Vector3(0f, 1.90f, 0f), 0.30f, 0.80f, robe, 8, Quaternion.Euler(6f, 0f, 8f));

            var orbPivot = new GameObject("OrbPivot");
            orbPivot.transform.SetParent(p, false);
            orbPivot.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            Shapes.Ball(orbPivot.transform, new Vector3(0.78f, 0f, 0f), 0.30f, Glow(Palette.Wizard, 4f));
            Spin(orbPivot, new Vector3(0f, 150f, 0f));

            Shapes.Box(p, new Vector3(-0.5f, 1.05f, 0.18f), new Vector3(0.14f, 0.14f, 0.5f), robe,
                Quaternion.Euler(-30f, 0f, 0f));
        }

        // ----------------------------------------------------------- baby dragon

        static void BuildBabyDragon(Transform p)
        {
            var hide = Flesh(Palette.BabyDragon);
            var belly = Flesh(new Color(1f, 0.78f, 0.45f));
            var fire = Glow(Palette.BabyDragon, 4f);
            var dark = Dark(0.08f);

            Shapes.Prim(PrimitiveType.Sphere, p, new Vector3(0f, 0f, 0f), new Vector3(0.85f, 0.75f, 1.15f), hide);
            Shapes.Prim(PrimitiveType.Sphere, p, new Vector3(0f, -0.18f, 0.05f), new Vector3(0.62f, 0.42f, 0.85f), belly);

            Shapes.Ball(p, new Vector3(0f, 0.22f, 0.62f), 0.52f, hide);
            Shapes.Cone(p, new Vector3(0f, 0.16f, 0.80f), 0.20f, 0.46f, hide, 6, Quaternion.Euler(90f, 0f, 0f));
            Shapes.Ball(p, new Vector3(0f, 0.14f, 0.95f), 0.16f, fire);

            Shapes.Cone(p, new Vector3(-0.16f, 0.42f, 0.56f), 0.06f, 0.26f, dark, 4, Quaternion.Euler(-24f, 0f, 14f));
            Shapes.Cone(p, new Vector3(0.16f, 0.42f, 0.56f), 0.06f, 0.26f, dark, 4, Quaternion.Euler(-24f, 0f, -14f));

            Shapes.Box(p, new Vector3(-0.12f, 0.30f, 0.74f), new Vector3(0.10f, 0.10f, 0.05f), fire);
            Shapes.Box(p, new Vector3(0.12f, 0.30f, 0.74f), new Vector3(0.10f, 0.10f, 0.05f), fire);

            var leftWing = new GameObject("WingL");
            leftWing.transform.SetParent(p, false);
            leftWing.transform.localPosition = new Vector3(-0.34f, 0.22f, -0.05f);
            Shapes.Blade(leftWing.transform, new Vector3(-0.62f, 0.16f, 0f), new Vector2(1.30f, 0.85f), hide,
                Quaternion.Euler(70f, 0f, 20f));
            Flap(leftWing, 1f);

            var rightWing = new GameObject("WingR");
            rightWing.transform.SetParent(p, false);
            rightWing.transform.localPosition = new Vector3(0.34f, 0.22f, -0.05f);
            Shapes.Blade(rightWing.transform, new Vector3(0.62f, 0.16f, 0f), new Vector2(1.30f, 0.85f), hide,
                Quaternion.Euler(70f, 0f, -20f));
            Flap(rightWing, -1f);

            Shapes.Box(p, new Vector3(0f, 0.02f, -0.72f), new Vector3(0.26f, 0.22f, 0.44f), hide);
            Shapes.Box(p, new Vector3(0f, 0.02f, -1.02f), new Vector3(0.17f, 0.15f, 0.34f), hide);
            Shapes.Cone(p, new Vector3(0f, 0.02f, -1.20f), 0.13f, 0.34f, fire, 4, Quaternion.Euler(-90f, 0f, 0f));
        }

        // --------------------------------------------------------------- gargoyle

        static void BuildGargoyle(Transform p)
        {
            var rock = Stone(Palette.Gargoyle);
            var dark = Dark(0.09f);
            var eye = Glow(Palette.Gargoyle, 4.5f);

            Shapes.Box(p, new Vector3(0f, 1.30f, 0f), new Vector3(1.35f, 1.15f, 0.85f), rock);
            Shapes.Box(p, new Vector3(0f, 1.92f, 0f), new Vector3(1.05f, 0.22f, 0.70f), dark);

            Shapes.Box(p, new Vector3(0f, 2.24f, 0.06f), new Vector3(0.66f, 0.52f, 0.62f), rock);
            Shapes.Box(p, new Vector3(0f, 2.24f, 0.36f), new Vector3(0.46f, 0.10f, 0.06f), eye);
            Shapes.Cone(p, new Vector3(-0.26f, 2.46f, 0f), 0.11f, 0.44f, rock, 4, Quaternion.Euler(-16f, 0f, 22f));
            Shapes.Cone(p, new Vector3(0.26f, 2.46f, 0f), 0.11f, 0.44f, rock, 4, Quaternion.Euler(-16f, 0f, -22f));

            Shapes.Blade(p, new Vector3(-0.95f, 1.75f, -0.35f), new Vector2(1.45f, 1.30f), rock,
                Quaternion.Euler(0f, 42f, 22f));
            Shapes.Blade(p, new Vector3(0.95f, 1.75f, -0.35f), new Vector2(1.45f, 1.30f), rock,
                Quaternion.Euler(0f, -42f, -22f));

            Shapes.Box(p, new Vector3(-0.88f, 1.10f, 0.18f), new Vector3(0.36f, 0.86f, 0.36f), rock,
                Quaternion.Euler(12f, 0f, 8f));
            Shapes.Box(p, new Vector3(0.88f, 1.10f, 0.18f), new Vector3(0.36f, 0.86f, 0.36f), rock,
                Quaternion.Euler(12f, 0f, -8f));

            Shapes.Box(p, new Vector3(-0.36f, 0.36f, 0f), new Vector3(0.46f, 0.74f, 0.52f), rock);
            Shapes.Box(p, new Vector3(0.36f, 0.36f, 0f), new Vector3(0.46f, 0.74f, 0.52f), rock);
            Shapes.Box(p, new Vector3(0f, 1.30f, 0.44f), new Vector3(0.55f, 0.55f, 0.08f), eye);
        }

        // ------------------------------------------------------------------- lich

        static void BuildLich(Transform p)
        {
            var robe = MatLib.Lit(new Color(0.12f, 0.11f, 0.18f), default, 0.2f);
            var bone = MatLib.Lit(Palette.Lich * 0.75f, default, 0.3f);
            var soul = Glow(Palette.Wizard, 5f);
            var crown = Glow(Palette.Lich, 3.5f);

            Shapes.Cone(p, new Vector3(0f, 0.1f, 0f), 1.05f, 2.60f, robe, 10);
            Shapes.Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 1.35f, 0f), new Vector3(1.05f, 0.05f, 1.05f), soul);

            Shapes.Ball(p, new Vector3(0f, 2.92f, 0f), 0.66f, bone);
            Shapes.Box(p, new Vector3(-0.16f, 2.98f, 0.30f), new Vector3(0.14f, 0.14f, 0.08f), soul);
            Shapes.Box(p, new Vector3(0.16f, 2.98f, 0.30f), new Vector3(0.14f, 0.14f, 0.08f), soul);
            Shapes.Box(p, new Vector3(0f, 2.68f, 0.28f), new Vector3(0.30f, 0.10f, 0.08f), bone);

            var crownPivot = new GameObject("Crown");
            crownPivot.transform.SetParent(p, false);
            crownPivot.transform.localPosition = new Vector3(0f, 3.42f, 0f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Shapes.Cone(crownPivot.transform, new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f),
                    0.09f, 0.42f, crown, 4);
            }
            Spin(crownPivot, new Vector3(0f, 45f, 0f));

            var hands = new GameObject("Hands");
            hands.transform.SetParent(p, false);
            hands.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            Shapes.Ball(hands.transform, new Vector3(-1.15f, 0f, 0.35f), 0.40f, soul);
            Shapes.Ball(hands.transform, new Vector3(1.15f, 0f, 0.35f), 0.40f, soul);
            Bob(hands, 0.16f, 1.7f);

            Shapes.Blade(p, new Vector3(-0.75f, 1.55f, -0.55f), new Vector2(0.9f, 2.6f), robe,
                Quaternion.Euler(0f, 28f, 8f));
            Shapes.Blade(p, new Vector3(0.75f, 1.55f, -0.55f), new Vector2(0.9f, 2.6f), robe,
                Quaternion.Euler(0f, -28f, -8f));
        }

        // ------------------------------------------------------------- animators

        static void Bob(GameObject go, float amplitude, float speed)
        {
            var b = go.AddComponent<PartBob>();
            b.Setup(amplitude, speed);
        }

        static void Spin(GameObject go, Vector3 degreesPerSecond)
        {
            var s = go.AddComponent<PartSpin>();
            s.Setup(degreesPerSecond);
        }

        static void Flap(GameObject go, float direction)
        {
            var f = go.AddComponent<PartFlap>();
            f.Setup(direction);
        }
    }

    public class PartBob : MonoBehaviour
    {
        Vector3 _origin;
        float _amplitude, _speed, _phase;

        public void Setup(float amplitude, float speed)
        {
            _amplitude = amplitude;
            _speed = speed;
            _phase = Random.value * 10f;
            _origin = transform.localPosition;
        }

        void Update()
        {
            transform.localPosition = _origin + Vector3.up * (Mathf.Sin(Time.time * _speed + _phase) * _amplitude);
        }
    }

    public class PartSpin : MonoBehaviour
    {
        Vector3 _rate;

        public void Setup(Vector3 degreesPerSecond) => _rate = degreesPerSecond;

        void Update() => transform.localRotation *= Quaternion.Euler(_rate * Time.deltaTime);
    }

    public class PartFlap : MonoBehaviour
    {
        float _dir, _phase;

        public void Setup(float direction)
        {
            _dir = direction;
            _phase = Random.value * 6f;
        }

        void Update()
        {
            float a = Mathf.Sin(Time.time * 11f + _phase) * 34f;
            transform.localRotation = Quaternion.Euler(0f, 0f, a * _dir);
        }
    }
}
