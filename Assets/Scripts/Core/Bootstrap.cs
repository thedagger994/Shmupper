using UnityEngine;

namespace Shmupper
{
    /// Entry point.
    ///
    /// Shmupper builds itself entirely from code, so there is nothing to wire up in a scene and
    /// nothing to forget to drag into an inspector slot. This hook runs after whatever scene
    /// loads, clears out any template camera and light left over from the Unity project
    /// template, and starts the game. Press Play on any scene and it works.
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;

            ClearTemplateObjects();

            var go = new GameObject("Shmupper");
            go.AddComponent<GameManager>();
        }

        /// The default URP scene ships with a camera, a directional light and a global volume.
        /// The volume is kept - its bloom is what makes the emissive art style work - but the
        /// camera and light would fight with the ones the game creates.
        static void ClearTemplateObjects()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam != null) Object.Destroy(cam.gameObject);

            foreach (var light in Object.FindObjectsByType<Light>())
                if (light != null) Object.Destroy(light.gameObject);

            foreach (var listener in Object.FindObjectsByType<AudioListener>())
                if (listener != null) Object.Destroy(listener);

            foreach (var system in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>())
                if (system != null) Object.Destroy(system.gameObject);
        }
    }
}
