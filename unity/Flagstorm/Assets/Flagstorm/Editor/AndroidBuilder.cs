using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Flagstorm.EditorTools
{
    // Compila el APK de desarrollo para la prueba de juego (tarea 12) sin pasos manuales.
    // Uso en lote: Unity.exe -batchmode -quit -projectPath unity/Flagstorm
    //   -executeMethod Flagstorm.EditorTools.AndroidBuilder.BuildDevelopmentApk
    public static class AndroidBuilder
    {
        // Dentro de unity/Flagstorm/Builds, ignorado por git.
        public const string OutputPath = "Builds/Android/flagstorm-dev.apk";

        [MenuItem("Flagstorm/Compilar APK de desarrollo")]
        public static void BuildDevelopmentApk()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // IL2CPP + ARM64: muchos teléfonos recientes ya no ejecutan apps de 32 bits.
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { PrototypeSceneBuilder.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Flagstorm] APK: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, "
                + $"{summary.totalErrors} errores, en {summary.outputPath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
