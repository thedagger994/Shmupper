using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Enemy condition, read entirely off the body.
    ///
    /// There are no health bars anywhere in Shmupper. With twenty enemies on screen a bar per
    /// enemy would bury the crosshair, so damage is shown on the thing taking it: the glowing
    /// accents that make each silhouette identifiable dim and slide toward a dying red, the
    /// unlit parts darken, and anything below a quarter health flickers. A player reads the room
    /// the same way they read a health bar, but without ever leaving the world.
    ///
    /// Everything is driven through a MaterialPropertyBlock because the whole horde shares
    /// materials - writing to a material would change every enemy of that kind at once.
    [DisallowMultipleComponent]
    public class EnemyAppearance : MonoBehaviour
    {
        struct Piece
        {
            public Renderer Renderer;
            public Color BaseColor;
            public Color Emission;
            public bool Emissive;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        /// Colour everything decays toward. Deliberately a deep ember rather than black, so a
        /// nearly dead enemy still reads as a threat and not as scenery.
        static readonly Color DyingTint = new Color(0.55f, 0.10f, 0.09f);

        const float FlickerBelow = 0.25f;
        const float FlashDuration = 0.09f;

        readonly List<Piece> _pieces = new List<Piece>();
        MaterialPropertyBlock _mpb;

        float _health01 = 1f;
        float _flashTimer;
        float _flickerSeed;
        bool _dirty = true;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _flickerSeed = Random.value * 64f;
        }

        /// Called once the body exists. Safe to call again if the body is rebuilt.
        public void Capture()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            _pieces.Clear();

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.sharedMaterial;
                if (mat == null) continue;

                var piece = new Piece
                {
                    Renderer = r,
                    BaseColor = Color.white,
                    Emission = Color.black,
                    Emissive = false
                };

                if (mat.HasProperty(BaseColorId)) piece.BaseColor = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorId)) piece.BaseColor = mat.GetColor(ColorId);

                if (mat.HasProperty(EmissionId))
                {
                    Color e = mat.GetColor(EmissionId);
                    if (e.maxColorComponent > 0.01f)
                    {
                        piece.Emission = e;
                        piece.Emissive = true;
                    }
                }

                _pieces.Add(piece);
            }

            _dirty = true;
        }

        public void SetHealth(float fraction01)
        {
            fraction01 = Mathf.Clamp01(fraction01);
            if (Mathf.Abs(fraction01 - _health01) < 0.001f) return;

            _health01 = fraction01;
            _dirty = true;
        }

        public void Flash()
        {
            _flashTimer = FlashDuration;
            _dirty = true;
        }

        void LateUpdate()
        {
            bool flickering = _health01 <= FlickerBelow;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                _dirty = true;
            }

            // A healthy enemy at rest costs nothing: the block is only rewritten when something
            // actually changed, or while a dying enemy is guttering.
            if (!_dirty && !flickering) return;
            _dirty = false;

            float flash = _flashTimer > 0f ? Mathf.Clamp01(_flashTimer / FlashDuration) : 0f;

            // Condition ramps in fast at first so the very first hit is visible, then eases.
            float wear = 1f - Mathf.Sqrt(_health01);

            float flicker = 1f;
            if (flickering)
            {
                float t = Time.time * 22f + _flickerSeed;
                float noise = Mathf.PerlinNoise(t, _flickerSeed);
                float depth = 1f - Mathf.InverseLerp(0f, FlickerBelow, _health01);
                flicker = Mathf.Lerp(1f, 0.35f + noise * 0.75f, depth);
            }

            Apply(wear, flash, flicker);
        }

        void Apply(float wear, float flash, float flicker)
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Renderer == null) continue;

                // Unlit body panels just get darker and a little bloodier.
                Color body = Color.Lerp(piece.BaseColor, piece.BaseColor * 0.30f + DyingTint * 0.35f, wear);
                if (flash > 0f) body = Color.Lerp(body, Color.white, flash);

                piece.Renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, body);
                _mpb.SetColor(ColorId, body);

                if (piece.Emissive)
                {
                    // The signature glow is the loudest part of every silhouette, so it carries
                    // most of the information: it shifts hue toward the dying tint, loses
                    // intensity, and gutters once the enemy is nearly finished.
                    Color hue = Color.Lerp(piece.Emission, DyingTint * piece.Emission.maxColorComponent, wear * 0.85f);
                    float intensity = Mathf.Lerp(1f, 0.28f, wear) * flicker;

                    Color emissive = hue * intensity;
                    if (flash > 0f) emissive += Color.white * (flash * 2.5f);

                    _mpb.SetColor(EmissionId, emissive);
                }

                piece.Renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
