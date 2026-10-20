using UnityEngine;

namespace Shmupper
{
    /// Generates the castle's stonework as a greyscale detail map.
    ///
    /// It has to tile seamlessly, because wall UVs are projected from world position and a single
    /// corridor crosses many repeats. Mathf.PerlinNoise cannot do that - it has no period - so
    /// this uses hash-based value noise on an integer lattice whose indices wrap, which is
    /// seamless by construction. Brick courses divide the texture evenly for the same reason.
    public static class StoneTexture
    {
        static Texture2D _cached;

        public static Texture2D Get()
        {
            if (_cached == null) _cached = Create();
            return _cached;
        }

        public static Texture2D Create(int size = 256, int seed = 7, int courses = 8, int perCourse = 4)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = "CastleStone",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            int brickH = size / courses;
            int brickW = size / perCourse;
            const int mortar = 4;

            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                int course = y / brickH;

                // Every other course is offset by half a brick. Courses must be an even number
                // for that alternation to meet correctly across the vertical seam.
                int offset = (course % 2 == 0) ? 0 : brickW / 2;

                for (int x = 0; x < size; x++)
                {
                    int bx = ((x + offset) % size) / brickW;
                    int inX = ((x + offset) % size) % brickW;
                    int inY = y % brickH;

                    // Per-brick base tone, so the wall reads as individually cut blocks.
                    float brickTone = 0.72f + Hash(bx, course, seed) * 0.28f;

                    // Two octaves of wrapping noise for surface grain.
                    float grain = Noise(x / 16f, y / 16f, size / 16, seed) * 0.55f
                                + Noise(x / 6f, y / 6f, size / 6, seed + 31) * 0.45f;
                    float tone = brickTone * (0.80f + grain * 0.34f);

                    // Mortar recess, darkened and softened at the edges so blocks look bevelled
                    // rather than stamped on.
                    float edge = Mathf.Min(
                        Mathf.Min(inX, brickW - 1 - inX),
                        Mathf.Min(inY, brickH - 1 - inY));

                    if (edge < mortar)
                    {
                        float t = edge / (float)mortar;
                        tone *= Mathf.Lerp(0.42f, 1f, t * t);
                    }

                    // Occasional chipped block keeps a large wall from reading as a repeat.
                    if (Hash(bx, course, seed + 97) > 0.93f) tone *= 0.78f;

                    byte v = (byte)(Mathf.Clamp01(tone) * 255f);
                    pixels[y * size + x] = new Color32(v, v, v, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }

        /// Deterministic 0-1 hash of two integers.
        static float Hash(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }

        /// Bilinear value noise whose lattice wraps at `period`, making it tileable.
        static float Noise(float x, float y, int period, int seed)
        {
            if (period < 1) period = 1;

            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;

            // Smoothstep the interpolant: linear blending leaves visible lattice creases.
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            int xa = Mod(x0, period), xb = Mod(x0 + 1, period);
            int ya = Mod(y0, period), yb = Mod(y0 + 1, period);

            float v00 = Hash(xa, ya, seed), v10 = Hash(xb, ya, seed);
            float v01 = Hash(xa, yb, seed), v11 = Hash(xb, yb, seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy);
        }

        static int Mod(int v, int m) => ((v % m) + m) % m;
    }
}
