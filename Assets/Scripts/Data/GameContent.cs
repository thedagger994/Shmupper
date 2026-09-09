using UnityEngine;

namespace Shmupper
{
    /// The bridge between authored assets and the runtime.
    ///
    /// Every prefab the game spawns is looked up here rather than constructed in code, so the
    /// bestiary and the arsenal can be opened, tuned and re-coloured in the editor without
    /// touching a script or entering Play mode. The asset lives at Assets/Resources/GameContent
    /// so it resolves from any scene.
    ///
    /// Every lookup is allowed to return null. When it does, the caller falls back to the
    /// original code path that builds the object from primitives, which means the game still
    /// runs on a fresh clone before anyone has pressed Shmupper > Forge Assets, and a prefab
    /// someone deletes by accident degrades to the built-in shape instead of a null reference.
    [CreateAssetMenu(fileName = "GameContent", menuName = "Shmupper/Game Content")]
    public class GameContent : ScriptableObject
    {
        public const string ResourcePath = "GameContent";

        [Header("Bestiary - one per EnemyKind, in enum order")]
        public Enemy[] EnemyPrefabs = new Enemy[6];

        [Header("Arsenal - one viewmodel per WeaponId, in enum order")]
        public GameObject[] WeaponPrefabs = new GameObject[4];

        // Projectiles, pickups, the wayshrine and the level's torches and pillars are still built
        // in code. They are the next candidates for the forge; they are left out of the registry
        // rather than sitting here as empty slots that suggest otherwise.

        static GameContent _instance;
        static bool _searched;

        /// Resolved once and cached. The miss is cached too - a project with no forged assets
        /// should not hit Resources on every single spawn.
        public static GameContent Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (_searched) return null;

                _searched = true;
                _instance = Resources.Load<GameContent>(ResourcePath);
                return _instance;
            }
        }

        /// Lets a scene override the asset, and lets tests and the forge clear the cache.
        public static void Use(GameContent content)
        {
            _instance = content;
            _searched = true;
        }

        public static void ClearCache()
        {
            _instance = null;
            _searched = false;
        }

        public Enemy EnemyPrefab(EnemyKind kind)
        {
            int i = (int)kind;
            if (EnemyPrefabs == null || i < 0 || i >= EnemyPrefabs.Length) return null;
            return EnemyPrefabs[i];
        }

        public GameObject WeaponPrefab(WeaponId id)
        {
            int i = (int)id;
            if (WeaponPrefabs == null || i < 0 || i >= WeaponPrefabs.Length) return null;
            return WeaponPrefabs[i];
        }
    }
}
