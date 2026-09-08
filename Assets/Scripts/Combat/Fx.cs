using UnityEngine;

namespace Shmupper
{
    /// All feedback in the game is geometry, not particles. Sparks are small emissive cubes
    /// thrown along the hit normal that shrink to nothing; explosions are an expanding shell.
    /// It costs almost nothing, needs no imported assets, and reads clearly against the flat
    /// stone walls.
    public static class Fx
    {
        public static void Impact(Vector3 point, Vector3 normal, Color color, int sparks = 5, float scale = 1f)
        {
            var root = new GameObject("Impact");
            root.transform.position = point;

            var mat = MatLib.Lit(color * 0.3f, color * 3f);

            for (int i = 0; i < sparks; i++)
            {
                Vector3 dir = (normal + Random.insideUnitSphere * 0.85f).normalized;
                var go = Shapes.Box(root.transform, Vector3.zero, Vector3.one * (0.12f * scale), mat,
                    Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f));

                var mover = go.AddComponent<FxShard>();
                mover.Launch(dir * Random.Range(4f, 11f) * scale, Random.Range(0.18f, 0.4f));
            }

            Object.Destroy(root, 0.7f);
        }

        public static void Burst(Vector3 point, Color color, float radius, float life = 0.35f)
        {
            var go = Shapes.Ball(null, point, radius * 0.35f, MatLib.Lit(color * 0.25f, color * 4f));
            go.name = "Burst";
            go.transform.position = point;

            var pulse = go.AddComponent<FxPulse>();
            pulse.Play(radius * 0.35f, radius * 2f, life);
        }

        /// Flat ground ring. Used to telegraph area attacks - the player needs to see the shape
        /// of the danger before it lands, not after.
        public static void Ring(Vector3 center, float radius, Color color, float life, bool growing = true)
        {
            var go = Shapes.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.06f,
                new Vector3(radius * 2f, 0.02f, radius * 2f), MatLib.Lit(color * 0.3f, color * 2.6f));
            go.name = "Ring";
            go.transform.position = center + Vector3.up * 0.06f;

            var anim = go.AddComponent<FxRing>();
            anim.Play(radius, life, growing);
        }

        public static void Explosion(Vector3 point, float radius, Color color)
        {
            Burst(point, color, radius, 0.45f);
            Impact(point, Vector3.up, color, 12, 1.6f);
        }

        /// Hitscan weapons need a visible line of flight or they feel like they do nothing. A
        /// stretched emissive box that collapses over two frames reads as a bolt of light.
        public static void Tracer(Vector3 from, Vector3 to, Color color, float width = 0.05f)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.05f) return;

            var go = Shapes.Box(null, Vector3.zero, new Vector3(width, width, length),
                MatLib.Lit(color * 0.3f, color * 3.5f));
            go.name = "Tracer";
            go.transform.position = from + delta * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(delta);

            var pulse = go.AddComponent<FxTracer>();
            pulse.Play(0.07f);
        }

        public static void KillSpark(Vector3 point, Color color)
        {
            Impact(point, Vector3.up, color, 10, 1.2f);
            Burst(point, color, 1.6f, 0.3f);
        }
    }

    public class FxShard : MonoBehaviour
    {
        Vector3 _velocity;
        float _life;
        float _maxLife;
        Vector3 _startScale;

        public void Launch(Vector3 velocity, float life)
        {
            _velocity = velocity;
            _maxLife = life;
            _life = life;
            _startScale = transform.localScale;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            _velocity += Physics.gravity * (Time.deltaTime * 0.5f);
            transform.position += _velocity * Time.deltaTime;
            transform.localScale = _startScale * Mathf.Clamp01(_life / _maxLife);
        }
    }

    public class FxRing : MonoBehaviour
    {
        float _radius, _life, _maxLife;
        bool _growing;

        public void Play(float radius, float life, bool growing)
        {
            _radius = radius;
            _maxLife = life;
            _life = life;
            _growing = growing;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = 1f - (_life / _maxLife);
            float scale = _growing ? Mathf.Lerp(0.15f, 1f, t) : Mathf.Lerp(1f, 0.15f, t);
            transform.localScale = new Vector3(_radius * 2f * scale, 0.02f, _radius * 2f * scale);
        }
    }

    public class FxTracer : MonoBehaviour
    {
        float _life, _maxLife;
        Vector3 _startScale;

        public void Play(float life)
        {
            _maxLife = life;
            _life = life;
            _startScale = transform.localScale;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = _life / _maxLife;
            transform.localScale = new Vector3(_startScale.x * t, _startScale.y * t, _startScale.z);
        }
    }

    public class FxPulse : MonoBehaviour
    {
        float _from, _to, _life, _maxLife;

        public void Play(float from, float to, float life)
        {
            _from = from;
            _to = to;
            _maxLife = life;
            _life = life;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = 1f - (_life / _maxLife);
            float size = Mathf.Lerp(_from, _to, t);
            transform.localScale = Vector3.one * size * (1f - t * 0.55f);
        }
    }
}
