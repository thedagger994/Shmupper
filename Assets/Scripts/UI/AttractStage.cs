using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shmupper
{
    /// The diorama behind the attract loop: the whole bestiary on a slow turntable, lit by two
    /// torches, with the camera drifting around it. An attract screen should show the game, and
    /// since every enemy in Shmupper is built from primitives at runtime, showing the real models
    /// costs nothing but a turntable.
    public class AttractStage : MonoBehaviour
    {
        static readonly EnemyKind[] Cast =
        {
            EnemyKind.Imp, EnemyKind.HollowKnight, EnemyKind.Wizard,
            EnemyKind.BabyDragon, EnemyKind.Gargoyle, EnemyKind.Lich
        };

        Transform _turntable;
        Camera _camera;
        float _time;

        public Camera StageCamera => _camera;

        /// Built a long way under the world so it can stay loaded while a floor exists above it
        /// without either one seeing the other.
        public static AttractStage Create()
        {
            var go = new GameObject("AttractStage");
            go.transform.position = new Vector3(0f, -600f, 0f);

            var stage = go.AddComponent<AttractStage>();
            stage.Build();
            return stage;
        }

        void Build()
        {
            var stone = MatLib.Lit(new Color(0.20f, 0.20f, 0.25f), default, 0.1f);
            var trim = MatLib.Lit(Palette.ArcaneLight * 0.2f, Palette.ArcaneLight * 1.6f);

            Shapes.Prim(PrimitiveType.Cylinder, transform, Vector3.zero, new Vector3(16f, 0.4f, 16f), stone);
            Shapes.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.42f, 0f), new Vector3(14f, 0.06f, 14f), trim);

            var table = new GameObject("Turntable");
            table.transform.SetParent(transform, false);
            table.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _turntable = table.transform;

            for (int i = 0; i < Cast.Length; i++)
            {
                float a = i / (float)Cast.Length * Mathf.PI * 2f;

                var pedestal = new GameObject("Display_" + Cast[i]);
                pedestal.transform.SetParent(_turntable, false);
                pedestal.transform.localPosition = new Vector3(Mathf.Cos(a) * 5.6f, 0f, Mathf.Sin(a) * 5.6f);
                pedestal.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);

                float scale = Cast[i] == EnemyKind.Lich ? 0.85f : 1.25f;
                var model = EnemyBuilder.Build(Cast[i], pedestal.transform);
                model.localScale = Vector3.one * scale;
            }

            AddTorch(new Vector3(-7.5f, 3.4f, -7.5f), Palette.TorchLight);
            AddTorch(new Vector3(7.5f, 3.4f, 7.5f), Palette.ArcaneLight);

            var camGo = new GameObject("AttractCamera");
            camGo.transform.SetParent(transform, false);

            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.02f, 0.02f, 0.035f);
            _camera.fieldOfView = 55f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 400f;

            var camData = _camera.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        void AddTorch(Vector3 localPosition, Color color)
        {
            var go = new GameObject("StageTorch");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            Shapes.Ball(go.transform, Vector3.zero, 0.6f, MatLib.Lit(color * 0.2f, color * 3f));

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 26f;
            light.intensity = 4.2f;
            light.shadows = LightShadows.None;

            go.AddComponent<TorchFlicker>().Setup(light, go.transform.GetChild(0), 4.2f);
        }

        void Update()
        {
            if (!isActiveAndEnabled) return;

            _time += Time.unscaledDeltaTime;

            if (_turntable != null)
                _turntable.localRotation = Quaternion.Euler(0f, _time * 12f, 0f);

            if (_camera != null)
            {
                float orbit = _time * 0.16f;
                float height = 4.6f + Mathf.Sin(_time * 0.35f) * 1.9f;

                _camera.transform.localPosition = new Vector3(Mathf.Cos(orbit) * 13.5f, height, Mathf.Sin(orbit) * 13.5f);
                _camera.transform.LookAt(transform.position + Vector3.up * 2.4f);
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
