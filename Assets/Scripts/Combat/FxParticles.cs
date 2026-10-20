using UnityEngine;

namespace Shmupper
{
    /// Shared particle emitters for every spark, ember and splinter in the game.
    ///
    /// The previous approach spawned a handful of GameObjects per hit and destroyed them a
    /// fraction of a second later. With the Arcanoflux firing eleven times a second into a room
    /// of twenty enemies that is hundreds of allocations and destructions per second, all of it
    /// garbage. Two pooled ParticleSystems handle the same work with no allocation: particles are
    /// emitted manually through EmitParams, which lets one system serve every colour in the game
    /// rather than needing one per enemy type.
    ///
    /// Mesh particles rather than billboards, because little tumbling cubes match a game whose
    /// entire art direction is primitives.
    public static class FxParticles
    {
        static ParticleSystem _sparks;
        static ParticleSystem _embers;
        static Mesh _cube;
        static Transform _root;

        public static void Reset()
        {
            _sparks = null;
            _embers = null;
            _root = null;
        }

        static void EnsureBuilt()
        {
            if (_sparks != null && _embers != null) return;

            var host = new GameObject("FxParticles");
            Object.DontDestroyOnLoad(host);
            _root = host.transform;

            if (_cube == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cube = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(temp);
            }

            _sparks = Build("Sparks", 1400, 1.35f, 0.55f);
            _embers = Build("Embers", 600, -0.12f, 1.9f);
        }

        /// A negative gravity modifier is what makes embers drift upward off a torch.
        static ParticleSystem Build(string name, int capacity, float gravity, float maxLifetime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = capacity;
            main.gravityModifier = gravity;
            main.startLifetime = maxLifetime;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            // World space, because the emitter never moves but the particles must stay where
            // they were thrown rather than following the host object.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            // Fade and shrink over life. Both multiply the per-particle start values, so this
            // works for every colour and size passed through EmitParams.
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f)));

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-260f, 260f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = _cube;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ParticleMaterial();

            ps.Play();
            return ps;
        }

        static Material ParticleMaterial()
        {
            var shader = Shader.Find("Shmupper/Particle");
            if (shader == null) return MatLib.Unlit(Color.white);

            return new Material(shader) { name = "ShmupParticle" };
        }

        /// A burst of sparks thrown out along a surface normal. Used for every weapon impact.
        public static void Sparks(Vector3 position, Vector3 normal, Color color,
                                  int count = 8, float speed = 7f, float size = 0.085f)
        {
            EnsureBuilt();

            var p = new ParticleSystem.EmitParams();

            for (int i = 0; i < count; i++)
            {
                p.position = position;
                p.velocity = (normal + Random.insideUnitSphere * 0.85f).normalized
                             * Random.Range(speed * 0.35f, speed);
                p.startColor = color;
                p.startSize = size * Random.Range(0.55f, 1.5f);
                p.startLifetime = Random.Range(0.22f, 0.5f);
                _sparks.Emit(p, 1);
            }
        }

        /// Heavier, slower debris for explosions and deaths.
        public static void Debris(Vector3 position, Color color, int count = 14, float speed = 9f)
        {
            EnsureBuilt();

            var p = new ParticleSystem.EmitParams();

            for (int i = 0; i < count; i++)
            {
                p.position = position + Random.insideUnitSphere * 0.3f;
                p.velocity = (Vector3.up * 0.55f + Random.insideUnitSphere).normalized
                             * Random.Range(speed * 0.3f, speed);
                p.startColor = color;
                p.startSize = Random.Range(0.1f, 0.26f);
                p.startLifetime = Random.Range(0.45f, 0.95f);
                _sparks.Emit(p, 1);
            }
        }

        /// One drifting ember. Torches call this on a stagger rather than running a system each.
        public static void Ember(Vector3 position, Color color, float speed = 0.9f)
        {
            EnsureBuilt();

            var p = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.12f,
                velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.5f, 1f) * speed, Random.Range(-0.35f, 0.35f)),
                startColor = color,
                startSize = Random.Range(0.035f, 0.085f),
                startLifetime = Random.Range(0.9f, 1.9f)
            };

            _embers.Emit(p, 1);
        }
    }
}
