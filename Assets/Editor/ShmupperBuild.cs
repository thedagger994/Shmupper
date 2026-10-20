using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Shmupper.EditorTools
{
    /// Release builds, scripted so they are reproducible and can be driven headlessly.
    ///
    /// The WebGL settings here are not defaults - they are what itch.io actually needs. Unity
    /// ships WebGL with Brotli compression, which requires the host to send a matching
    /// Content-Encoding header; itch does not, and the result is a blank canvas with no error
    /// that anyone would recognise. Gzip plus the decompression fallback works everywhere at the
    /// cost of a slightly larger download.
    public static class ShmupperBuild
    {
        const string ScenePath = "Assets/Scenes/Shmupper.unity";
        const string WebGlOutput = "Builds/WebGL";
        const string WindowsOutput = "Builds/Windows/Shmupper.exe";

        [MenuItem("Shmupper/Build/WebGL (for itch.io)", false, 60)]
        public static void BuildWebGl()
        {
            ApplyCommonSettings();

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // The itch player embeds the canvas in an iframe, so the page must not try to manage
            // its own resolution dialog or run at a fixed size.
            PlayerSettings.runInBackground = false;

            Run(BuildTarget.WebGL, BuildTargetGroup.WebGL, WebGlOutput, BuildOptions.None);
        }

        [MenuItem("Shmupper/Build/Windows", false, 61)]
        public static void BuildWindows()
        {
            ApplyCommonSettings();
            Run(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, WindowsOutput, BuildOptions.None);
        }

        [MenuItem("Shmupper/Build/Both", false, 62)]
        public static void BuildBoth()
        {
            BuildWindows();
            BuildWebGl();
        }

        static void ApplyCommonSettings()
        {
            PlayerSettings.productName = "Shmupper";
            PlayerSettings.companyName = "DefaultCompany";

            // Linear colour is what makes the emissive art and bloom read correctly; gamma would
            // wash the whole grade out.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // The game locks the cursor and is played fullscreen or in an embed; there is no
            // reason to show Unity's old resolution dialog.
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            EnsureSceneInBuild();
        }

        /// A build with no scenes silently produces an empty player, so this is checked rather
        /// than assumed.
        static void EnsureSceneInBuild()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(s);

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void Run(BuildTarget target, BuildTargetGroup group, string output, BuildOptions options)
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Shmupper] No play scene at " + ScenePath + ". Run Shmupper > Forge Assets and Build Scene first.");
                return;
            }

            string directory = Path.GetExtension(output).Length > 0 ? Path.GetDirectoryName(output) : output;
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var player = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = options
            };

            var report = BuildPipeline.BuildPlayer(player);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(string.Format("[Shmupper] {0} build succeeded: {1} ({2:N1} MB, {3:N0}s)",
                    target, output, summary.totalSize / 1048576f, summary.totalTime.TotalSeconds));
            }
            else
            {
                Debug.LogError(string.Format("[Shmupper] {0} build {1} with {2} error(s).",
                    target, summary.result, summary.totalErrors));
            }
        }
    }
}
