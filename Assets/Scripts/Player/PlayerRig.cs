using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shmupper
{
    /// Assembles the Gunmage. Kept separate from the game manager so the rig can be described in
    /// one place: a capsule that walks, a wide-angle camera, a lantern so dynamic objects are lit
    /// even away from a torch, and the systems bolted to them.
    public class PlayerRig
    {
        public GameObject Root;
        public Camera Camera;
        public PlayerController Controller;
        public PlayerHealth Health;
        public WeaponSystem Weapons;

        public static PlayerRig Create(RunState run)
        {
            var rig = new PlayerRig();

            var root = new GameObject("Player");
            root.layer = Layers.Player;
            rig.Root = root;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.05f;
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.55f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 92f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 320f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.017f, 0.03f);
            rig.Camera = cam;

            // Bloom is doing most of the art direction here - every enemy tell, every bolt and
            // every torch is an emissive surface - so post-processing is switched on explicitly
            // rather than left at the runtime default.
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // The lantern is what keeps enemies readable between torches. It is deliberately dim
            // and short ranged so the castle still feels dark.
            var lantern = camGo.AddComponent<Light>();
            lantern.type = LightType.Point;
            lantern.color = new Color(1f, 0.92f, 0.78f);
            lantern.range = 17f;
            lantern.intensity = 1.35f;
            lantern.shadows = LightShadows.None;

            rig.Controller = root.AddComponent<PlayerController>();
            rig.Controller.Init(camGo.transform, run);

            rig.Health = root.AddComponent<PlayerHealth>();
            rig.Health.Init(run, rig.Controller);

            rig.Weapons = root.AddComponent<WeaponSystem>();
            rig.Weapons.Init(cam, rig.Controller, run);

            return rig;
        }

        public void SetInputEnabled(bool enabled)
        {
            if (Controller != null)
            {
                Controller.InputEnabled = enabled;
                Controller.LookEnabled = enabled;
            }

            if (Weapons != null) Weapons.InputEnabled = enabled;
            if (Health != null) Health.InputEnabled = enabled;
        }

        public void SetActive(bool active)
        {
            if (Root != null) Root.SetActive(active);
        }
    }
}
