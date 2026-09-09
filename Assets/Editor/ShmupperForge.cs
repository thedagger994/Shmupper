using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shmupper.EditorTools
{
    /// Bakes the game's code-built art into real, editable assets.
    ///
    /// The bestiary and the arsenal are defined once, as shape code in EnemyBuilder and
    /// WeaponView. Running this forge executes that code in edit mode, persists the meshes and
    /// materials it produced, and saves the result as prefabs wired up with colliders, stats and
    /// components. From then on the game instantiates those prefabs, and they can be recoloured,
    /// rescaled and retuned in the Inspector without touching a script or entering Play mode.
    ///
    /// It is safe to re-run at any time. Prefabs are overwritten in place, so anything else that
    /// references them keeps its link, and materials and meshes are reused by content hash rather
    /// than duplicated.
    public static class ShmupperForge
    {
        const string PrefabRoot = "Assets/Prefabs";
        const string MaterialRoot = "Assets/Art/Materials";
        const string MeshRoot = "Assets/Art/Meshes";
        const string ResourceRoot = "Assets/Resources";
        const string ScenePath = "Assets/Scenes/Shmupper.unity";

        [MenuItem("Shmupper/Forge Assets", false, 0)]
        public static void ForgeAll()
        {
            // Deliberately not wrapped in StartAssetEditing. Batching defers imports, and this
            // forge reads back assets it just wrote - materials it dedupes against, prefabs it
            // stores in the registry - which inside a batch return null.
            EnsureFolders();

            var content = LoadOrCreateContent();

            ForgeEnemies(content);
            ForgeWeapons(content);

            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameContent.ClearCache();
            Debug.Log("[Shmupper] Forge complete. Prefabs are in " + PrefabRoot +
                      ", registry at " + ResourceRoot + "/GameContent.asset");
        }

        [MenuItem("Shmupper/Forge Assets and Build Scene", false, 1)]
        public static void ForgeAndBuildScene()
        {
            ForgeAll();
            BuildScene();
        }

        // ------------------------------------------------------------------ bestiary

        static void ForgeEnemies(GameContent content)
        {
            var kinds = (EnemyKind[])System.Enum.GetValues(typeof(EnemyKind));
            if (content.EnemyPrefabs == null || content.EnemyPrefabs.Length != kinds.Length)
                content.EnemyPrefabs = new Enemy[kinds.Length];

            foreach (var kind in kinds)
            {
                var root = new GameObject("Enemy_" + kind);
                root.layer = Layers.Enemy;

                try
                {
                    var body = EnemyBuilder.Build(kind, root.transform);
                    var stats = EnemyStats.For(kind);

                    Enemy behaviour = AttachBehaviour(root, kind);
                    AttachCollision(root, kind, stats);
                    root.AddComponent<EnemyAppearance>();

                    StampStats(behaviour, stats, body);

                    Persist(root, "Enemy_" + kind);

                    string path = PrefabRoot + "/Enemies/Enemy_" + kind + ".prefab";
                    var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                    content.EnemyPrefabs[(int)kind] = saved != null ? saved.GetComponent<Enemy>() : null;
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        static Enemy AttachBehaviour(GameObject go, EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Imp:          return go.AddComponent<ImpEnemy>();
                case EnemyKind.HollowKnight: return go.AddComponent<HollowKnightEnemy>();
                case EnemyKind.Wizard:       return go.AddComponent<WizardEnemy>();
                case EnemyKind.BabyDragon:   return go.AddComponent<BabyDragonEnemy>();
                case EnemyKind.Gargoyle:     return go.AddComponent<GargoyleEnemy>();
                default:                     return go.AddComponent<LichBoss>();
            }
        }

        /// Mirrors what ConfigureCollision would do at spawn time, so the prefab is complete and
        /// the runtime call finds everything already present and simply reuses it.
        static void AttachCollision(GameObject go, EnemyKind kind, EnemyStats stats)
        {
            bool flying = kind == EnemyKind.BabyDragon || kind == EnemyKind.Lich;

            if (flying)
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.radius = stats.Radius;
                sphere.center = Vector3.up * 0.2f;

                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                var cc = go.AddComponent<CharacterController>();
                cc.radius = stats.Radius;
                cc.height = stats.Height;
                cc.center = new Vector3(0f, stats.Height * 0.5f, 0f);
                cc.slopeLimit = 55f;
                cc.stepOffset = 0.6f;
            }
        }

        /// Writes the stat table onto the prefab's private serialized fields, so the numbers are
        /// visible and editable in the Inspector rather than buried in a switch statement.
        static void StampStats(Enemy behaviour, EnemyStats stats, Transform body)
        {
            var so = new SerializedObject(behaviour);

            var authored = so.FindProperty("_authoredStats");
            if (authored != null)
            {
                authored.FindPropertyRelative("Health").floatValue = stats.Health;
                authored.FindPropertyRelative("Speed").floatValue = stats.Speed;
                authored.FindPropertyRelative("Damage").floatValue = stats.Damage;
                authored.FindPropertyRelative("Points").intValue = stats.Points;
                authored.FindPropertyRelative("Radius").floatValue = stats.Radius;
                authored.FindPropertyRelative("Height").floatValue = stats.Height;
            }

            var bodyProp = so.FindProperty("_authoredBody");
            if (bodyProp != null) bodyProp.objectReferenceValue = body;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ arsenal

        static void ForgeWeapons(GameContent content)
        {
            var ids = (WeaponId[])System.Enum.GetValues(typeof(WeaponId));
            if (content.WeaponPrefabs == null || content.WeaponPrefabs.Length != ids.Length)
                content.WeaponPrefabs = new GameObject[ids.Length];

            foreach (var id in ids)
            {
                var def = WeaponDef.Get(id);
                if (def == null) continue;

                GameObject root = WeaponView.BuildFromPrimitives(id, def);

                try
                {
                    var muzzle = new GameObject("Muzzle");
                    muzzle.transform.SetParent(root.transform, false);
                    muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.62f);

                    var flash = muzzle.AddComponent<Light>();
                    flash.type = LightType.Point;
                    flash.color = def.Tint;
                    flash.range = 12f;
                    flash.intensity = 0f;
                    flash.shadows = LightShadows.None;

                    Persist(root, "Weapon_" + id);

                    string path = PrefabRoot + "/Weapons/Weapon_" + id + ".prefab";
                    content.WeaponPrefabs[(int)id] = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        // ------------------------------------------------------- mesh and material persistence

        /// Everything the shape code produced is a scene-only object. Unless the meshes and
        /// materials are written to disk first, the saved prefab would reference objects that
        /// vanish the moment the forge finishes, and every renderer would come back pink.
        static void Persist(GameObject root, string label)
        {
            var meshCache = new Dictionary<Mesh, Mesh>();
            var materialCache = new Dictionary<Material, Material>();
            int meshIndex = 0;

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || EditorUtility.IsPersistent(mesh)) continue;

                if (!meshCache.TryGetValue(mesh, out var saved))
                {
                    string name = label + "_" + meshIndex;
                    meshIndex++;

                    saved = Object.Instantiate(mesh);
                    saved.name = name;
                    AssetDatabase.CreateAsset(saved, MeshRoot + "/" + name + ".asset");
                    meshCache[mesh] = saved;
                }

                filter.sharedMesh = saved;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mat = renderer.sharedMaterial;
                if (mat == null || EditorUtility.IsPersistent(mat)) continue;

                if (!materialCache.TryGetValue(mat, out var saved))
                {
                    saved = LoadOrCreateMaterial(mat);
                    materialCache[mat] = saved;
                }

                renderer.sharedMaterial = saved;
            }
        }

        /// Materials are keyed by their visible properties, so two enemies that happen to use the
        /// same stone grey share one asset instead of accumulating a folder of duplicates across
        /// repeated runs of the forge.
        static Material LoadOrCreateMaterial(Material source)
        {
            string path = MaterialRoot + "/" + MaterialName(source) + ".mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var copy = new Material(source);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        static string MaterialName(Material m)
        {
            Color baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                            : m.HasProperty("_Color") ? m.GetColor("_Color")
                            : Color.white;

            Color emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0f;
            float metal = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;

            string shader = m.shader != null && m.shader.name.Contains("Unlit") ? "U" : "L";

            return string.Format("M_{0}_{1}_{2}_s{3:00}_m{4:00}",
                shader, Hex(baseColor), Hex(emission),
                Mathf.RoundToInt(smooth * 99f), Mathf.RoundToInt(metal * 99f));
        }

        /// Emission is written in HDR and routinely exceeds one, so channels are compressed
        /// rather than clamped - otherwise every glowing material would hash to ffffff.
        static string Hex(Color c)
        {
            return string.Format("{0:x2}{1:x2}{2:x2}",
                Mathf.RoundToInt(Mathf.Clamp01(c.r / (1f + c.r)) * 255f),
                Mathf.RoundToInt(Mathf.Clamp01(c.g / (1f + c.g)) * 255f),
                Mathf.RoundToInt(Mathf.Clamp01(c.b / (1f + c.b)) * 255f));
        }

        // ------------------------------------------------------------------ registry and scene

        static GameContent LoadOrCreateContent()
        {
            string path = ResourceRoot + "/GameContent.asset";

            var content = AssetDatabase.LoadAssetAtPath<GameContent>(path);
            if (content != null) return content;

            content = ScriptableObject.CreateInstance<GameContent>();
            AssetDatabase.CreateAsset(content, path);
            return content;
        }

        [MenuItem("Shmupper/Build Play Scene", false, 20)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The game builds its own camera, lighting and UI at runtime, and Bootstrap clears
            // any leftovers from the scene, so authoring them here would only create something to
            // be destroyed on the first frame. The scene's job is to hold the manager and the
            // post-processing volume that makes the emissive art read.
            var manager = new GameObject("Shmupper");
            manager.AddComponent<GameManager>();

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(
                "Assets/Settings/SampleSceneProfile.asset");
            if (profile != null) volume.sharedProfile = profile;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[Shmupper] Play scene written to " + ScenePath);
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var s in scenes)
                if (s.path == path) return;

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Enemies");
            EnsureFolder("Assets/Prefabs/Weapons");
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Materials");
            EnsureFolder("Assets/Art/Meshes");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Scenes");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            string leaf = path.Substring(split + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
