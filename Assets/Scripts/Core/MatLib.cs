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

        public static void Clear()
        {
            Cache.Clear();
        }

        public static Material Lit(Color baseColor, Color emission = default, float smoothness = 0.15f, float metallic = 0f)
        {
            string key = "L" + baseColor + emission + smoothness + metallic;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(LitShader) { name = "Lit_" + key.GetHashCode() };
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

            var m = new Material(UnlitShader) { name = "Unlit_" + key.GetHashCode() };
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            Cache[key] = m;
            return m;
        }

        public static Material VertexLit(Color baseColor, Color ambient, float boost = 1f)
        {
            string key = "V" + baseColor + ambient + boost;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(VertexLitShader) { name = "VLit_" + key.GetHashCode() };
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetColor("_Ambient", ambient);
            m.SetFloat("_Boost", boost);
            Cache[key] = m;
            return m;
        }
    }
}
