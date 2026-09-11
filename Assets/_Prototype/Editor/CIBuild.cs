using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Prototype.Application
{
    public static class CIBuild
    {
        const string Scene = "Assets/_Prototype/Scenes/Prototype_Main.unity";

        public static void BuildWebGL() => Build(BuildTarget.WebGL,               "Artifacts/Build/WebGL");
        public static void BuildWin64() => Build(BuildTarget.StandaloneWindows64, "Artifacts/Build/Win/Farm.exe");
        public static void BuildOSX()   => Build(BuildTarget.StandaloneOSX,       "Artifacts/Build/OSX/Farm.app");

        static void Build(BuildTarget target, string outPath)
        {
            var buildNumber = Arg("-buildNumber") ?? "local";
            PlayerSettings.bundleVersion = $"proto-{buildNumber}";
            PlayerSettings.companyName   = "FarmProto";

            // Prototype: fast build matters more than small build
            if (target == BuildTarget.WebGL)
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.exceptionSupport  = WebGLExceptionSupport.FullWithStacktrace;
                EditorUserBuildSettings.development    = true;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            var opts = new BuildPlayerOptions {
                scenes           = new[] { Scene },
                locationPathName = outPath,
                target           = target,
                options          = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report  = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;

            UnityEngine.Debug.Log($"[CI] build={buildNumber} result={summary.result} " +
                      $"time={summary.totalTime} size={summary.totalSize} " +
                      $"errors={summary.totalErrors} warnings={summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var msg in step.messages.Where(m => m.type >= LogType.Error))
                        UnityEngine.Debug.LogError($"[CI][{step.name}] {msg.content}");
                EditorApplication.Exit(1);
            }

            File.WriteAllText("Artifacts/build-manifest.txt",
                $"build={buildNumber}\ntarget={target}\nutc={DateTime.UtcNow:o}\n" +
                $"commit={Environment.GetEnvironmentVariable("GIT_SHA") ?? "unknown"}\n");

            EditorApplication.Exit(0);
        }

        internal static string Arg(string name)
        {
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i < argv.Length - 1; i++)
                if (argv[i] == name) return argv[i + 1];
            return null;
        }

        /// <summary>
        /// Create the minimal Prototype_Main.unity scene if it does not exist.
        /// Lets the whole CLI pipeline bootstrap without a manual Editor step.
        /// </summary>
        public static void EnsureScene()
        {
            const string scenePath = Scene;
            if (File.Exists(scenePath))
            {
                UnityEngine.Debug.Log($"[CI] scene exists: {scenePath}");
                EditorApplication.Exit(0);
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var camera = new GameObject("Main Camera");
            camera.tag = "MainCamera"; // required so Camera.main resolves it at runtime (bug: blue screen otherwise)
            camera.AddComponent<Camera>();
            camera.AddComponent<UnityEngine.AudioListener>();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            UnityEngine.Debug.Log($"[CI] created scene: {scenePath}");
            EditorApplication.Exit(0);
        }
    }
}
