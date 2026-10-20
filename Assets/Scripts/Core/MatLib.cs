using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Runtime material cache. The whole game is built from code, so materials are created on
    /// demand and shared by key to keep batching sane. Shader lookups fall back gracefully so a
    /// missing custom shader degrades the look rather than breaking the build.
    public static class MatLib
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Shader _lit, _unlit, _vertexLit;

        public static Shader LitShader
        {
            get
            {
                if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null) _lit = Shader.Find("Standard");
                return _lit;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (_unlit == null) _unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
                return _unlit;
            }
        }

        public static Shader VertexLitShader
        {
            get
            {
                if (_vertexLit == null) _vertexLit = Shader.Find("Shmupper/VertexLit");
                if (_vertexLit == null) _vertexLit = UnlitShader;
                return _vertexLit;
            }
        }

        /// Never hand null to `new Material`.
        ///
        /// It throws, and because materials are built while the level is being constructed, that
        /// exception used to abort level construction halfway and leave an entirely black world
        /// with a working HUD painted over it. A wrongly-shaded wall is a cosmetic bug; a thrown
        /// exception here is a dead build. The forge registers the real shaders as always-included
        /// so this should never fire, but it is the difference between a blemish and a black
        /// screen if one ever goes missing again.
        static Shader Safe(Shader preferred, string context)
        {
            if (preferred != null) return preferred;

            var fallback = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Hidden/InternalErrorShader");

            Debug.LogError("Shmupper: shader missing for " + context +
                           ". It is probably not in Always Included Shaders, so it was stripped " +
                           "from this build. Run Shmupper > Register Required Shaders.");

            return fallback;
        }

        public static void Clear()
        {
            Cache.Clear();
        }

        public static Material Lit(Color baseColor, Color emission = default, float smoothness = 0.15f, float metallic = 0f)
        {
            string key = "L" + baseColor + emission + smoothness + metallic;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(Safe(LitShader, "MatLib.Lit")) { name = "Lit_" + key.GetHashCode() };
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            Cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color)
        {
            string key = "U" + color;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(Safe(UnlitShader, "MatLib.Unlit")) { name = "Unlit_" + key.GetHashCode() };
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            Cache[key] = m;
            return m;
        }

        public static Material VertexLit(Color baseColor, Color ambient, float boost = 1.25f,
                                         Texture2D stone = null, float detail = 1f)
        {
            string key = "V" + baseColor + ambient + boost + (stone != null ? stone.name : "none") + detail;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(Safe(VertexLitShader, "MatLib.VertexLit")) { name = "VLit_" + key.GetHashCode() };
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetColor("_Ambient", ambient);
            m.SetFloat("_Boost", boost);
            m.SetFloat("_DetailStrength", detail);

            if (stone != null) m.SetTexture("_BaseMap", stone);

            Cache[key] = m;
            return m;
        }
    }
}
