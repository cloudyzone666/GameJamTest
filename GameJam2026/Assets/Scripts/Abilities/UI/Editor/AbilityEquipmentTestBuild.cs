using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam2026.Abilities.UI.Editor
{
    // Builds the existing equipment scene without changing the project's scene list.
    public static class AbilityEquipmentTestBuild
    {
        private const string ScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";

        [MenuItem("GameJam2026/Abilities/Build Windows Equipment Test")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building the equipment test.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows x64 build support is not installed for this Unity Editor.");
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("The equipment test scene is missing.", ScenePath);
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save open scene changes before building the equipment test.");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string outputDirectory = Path.Combine(projectRoot, "Builds", "Windows", "AbilityEquipmentTest", stamp);
            if (Directory.Exists(outputDirectory))
                throw new IOException("The build output already exists: " + outputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "AbilityEquipmentTest.exe");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Equipment build failed: {report.summary.result}; errors: {report.summary.totalErrors}.");

            Debug.Log($"ABILITY_EQUIPMENT_WINDOWS_BUILD_SUCCESS: {executable}\n"
                + $"Scenes: {ScenePath}; bytes: {report.summary.totalSize}; time: {report.summary.totalTime}.");
        }
    }
}
