using UnityEngine;

namespace Shmupper
{
    /// All feedback in the game is geometry, not particles. Sparks are small emissive cubes
    /// thrown along the hit normal that shrink to nothing; explosions are an expanding shell.
    /// It costs almost nothing, needs no imported assets, and reads clearly against the flat
    /// stone walls.
    public static class Fx
    {
        /// Every impact in the game routes through the shared particle pool. This used to spawn a
        /// GameObject per spark and destroy it moments later, which at eleven shots a second into
        /// a crowded room meant hundreds of allocations and destructions every second.
        public static void Impact(Vector3 point, Vector3 normal, Color color, int sparks = 5, float scale = 1f)
        {
            FxParticles.Sparks(point, normal, color,
                Mathf.Max(1, Mathf.RoundToInt(sparks * 1.6f)),
                8f * scale,
                0.085f * scale);
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
            FxParticles.Debris(point, color, 18, 11f);
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
            FxParticles.Debris(point, color, 12, 7.5f);
        }
    }

}
