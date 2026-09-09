using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            ForgeVolumeProfile();
            ForgeFontAsset();

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

        // ------------------------------------------------------------------ typography

        const string FontTtfPath = "Assets/Art/Fonts/IMFellEnglish-Regular.ttf";
        const string FontAssetPath = "Assets/Resources/Fonts/IMFellEnglish SDF.asset";

        /// TextMeshPro needs its shaders and TMP_Settings present before any SDF text will draw,
        /// and they ship inside the ugui package as a .unitypackage rather than as assets. This
        /// unpacks them. It must run as its own editor session: the import completes after the
        /// current one finishes, so anything creating a font asset in the same run finds nothing.
        [MenuItem("Shmupper/Import TMP Essentials", false, 40)]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                Debug.Log("[Shmupper] TMP essentials already present.");
                return;
            }

            string package = null;
            foreach (var dir in System.IO.Directory.GetDirectories("Library/PackageCache"))
            {
                string candidate = System.IO.Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                if (System.IO.File.Exists(candidate)) { package = candidate; break; }
            }

            if (package == null)
            {
                Debug.LogError("[Shmupper] Could not find TMP Essential Resources.unitypackage in the package cache.");
                return;
            }

            AssetDatabase.ImportPackage(package, false);
            Debug.Log("[Shmupper] Importing TMP essentials from " + package);
        }

        /// Bakes the IM Fell English TTF into a signed distance field atlas. This is what fixes
        /// the blurry front end: legacy text baked one bitmap at one size, whereas an SDF atlas
        /// is resolution independent.
        public static TMPro.TMP_FontAsset ForgeFontAsset()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (ttf == null)
            {
                Debug.LogError("[Shmupper] Font missing at " + FontTtfPath);
                return null;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Resources/Fonts");

            var asset = TMPro.TMP_FontAsset.CreateFontAsset(
                ttf, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024,
                TMPro.AtlasPopulationMode.Dynamic,
                true);

            if (asset == null)
            {
                Debug.LogError("[Shmupper] CreateFontAsset returned null - are TMP essentials imported?");
                return null;
            }

            asset.name = "IMFellEnglish SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);

            // The atlas texture and material are created in memory alongside the asset; without
            // parenting them into the same file they are not saved and the font renders blank.
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    if (asset.atlasTextures[i] == null) continue;
                    asset.atlasTextures[i].name = "IMFellEnglish Atlas " + i;
                    AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
                }
            }

            if (asset.material != null)
            {
                asset.material.name = "IMFellEnglish SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            // Pre-render the characters the interface actually uses, so the atlas is populated in
            // the committed asset rather than being filled in on the fly on first launch.
            asset.TryAddCharacters(
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                " .,:;!?'\"()[]{}<>-_+=*/\\|@#$%^&~`");

            EditorUtility.SetDirty(asset);
            Debug.Log("[Shmupper] Font asset written to " + FontAssetPath);
            return asset;
        }

        /// The castle's resting look. This is the half of the post-processing that never moves;
        /// PostFx layers the reactive half on top at runtime, so anything animated in response to
        /// damage is deliberately left out of here.
        public static UnityEngine.Rendering.VolumeProfile ForgeVolumeProfile()
        {
            const string path = "Assets/Settings/ShmupperProfile.asset";

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            else
            {
                // Rebuild from scratch so re-running the forge is idempotent rather than
                // accumulating a second copy of every override.
                foreach (var existing in profile.components.ToArray())
                {
                    profile.Remove(existing.GetType());
                    Object.DestroyImmediate(existing, true);
                }
            }

            // Filmic response curve. Without it the emissive art clips to flat white the moment
            // two torches overlap.
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;

            // Bloom is what makes glowing eyes and rune bands read as light sources rather than
            // as bright paint. The threshold sits just under 1 so only genuinely emissive
            // surfaces bleed, and the tint warms it toward torchlight.
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 1.15f;
            bloom.scatter.value = 0.68f;
            bloom.tint.value = new Color(1f, 0.88f, 0.74f);
            bloom.highQualityFiltering.value = true;

            var color = profile.Add<ColorAdjustments>(false);
            color.contrast.overrideState = true;
            color.contrast.value = 16f;
            color.colorFilter.overrideState = true;
            color.colorFilter.value = new Color(0.98f, 0.96f, 1f);

            // Cool the stone down and let the torches provide the only warmth in frame.
            var balance = profile.Add<WhiteBalance>(true);
            balance.temperature.value = -12f;
            balance.tint.value = 4f;

            // Crushes the blacks toward blue and keeps highlights slightly amber, which is the
            // whole medieval-dungeon-at-night look in one component.
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.value = new Vector4(0.86f, 0.90f, 1.10f, 0f);
            smh.midtones.value = new Vector4(1f, 1f, 1f, 0f);
            smh.highlights.value = new Vector4(1.06f, 1.00f, 0.92f, 0f);

            // Deliberately light. Enough to take the digital edge off a fast turn, not enough to
            // smear a first person game into nausea.
            var blur = profile.Add<MotionBlur>(true);
            blur.mode.value = MotionBlurMode.CameraOnly;
            blur.quality.value = MotionBlurQuality.Medium;
            blur.intensity.value = 0.14f;
            blur.clamp.value = 0.04f;

            // VolumeProfile.Add creates each component in memory only. Without parenting them
            // into the profile asset they are never serialized, and the profile saves with a
            // list of null references - which looks like a working asset until nothing grades.
            foreach (var component in profile.components)
            {
                if (component == null || AssetDatabase.Contains(component)) continue;

                component.name = component.GetType().Name;
                component.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

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
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = ForgeVolumeProfile();

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
