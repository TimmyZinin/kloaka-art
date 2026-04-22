using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceShooter.EditorTools
{
    /// <summary>
    /// Command line build entry point:
    ///   Unity -batchmode -quit -projectPath . -buildTarget WebGL \
    ///         -executeMethod SpaceShooter.EditorTools.BuildScript.BuildWebGL \
    ///         -customBuildPath build/WebGL
    /// </summary>
    public static class BuildScript
    {
        public static void BuildWebGL()
        {
            string[] scenes = { "Assets/Scenes/SampleScene.unity" };
            string outputPath = GetArg("-customBuildPath") ?? "build/WebGL";

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var webgl = NamedBuildTarget.WebGL;
            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
            // Minimal managed stripping + disable engine code stripping entirely.
            // The whole game is constructed at runtime via
            // [RuntimeInitializeOnLoadMethod] and AddComponent<T>() — Unity's
            // stripper cannot see these usages from scene analysis.
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.Minimal);
            PlayerSettings.stripEngineCode = false;

            // Brotli: compresses .data/.wasm ~70%. decompressionFallback lets
            // GitHub Pages serve the .unityweb files without Content-Encoding
            // headers — loader decompresses in JS. Adds ~8 min to build time
            // but keeps every file under GitHub's 100MB limit.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.runInBackground = true;
            PlayerSettings.productName = "Kloaka Pechali";
            PlayerSettings.companyName = "Zinin Corp";

            EnsureAlwaysIncludedShaders(
                "Standard",
                "Unlit/Color",
                "Unlit/Texture",
                "Sprites/Default",
                "Legacy Shaders/Diffuse",
                "SpaceShooter/AnimatedFloor",
                "Hidden/SpaceShooter/Outline",
                "Hidden/SpaceShooter/PostFX",
                "Skybox/Panoramic");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildScript] Result: {summary.result}");
            Debug.Log($"[BuildScript] Output : {summary.outputPath}");
            Debug.Log($"[BuildScript] Size   : {summary.totalSize / 1024 / 1024} MB");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed with result {summary.result}");
            }
        }

        private static void EnsureAlwaysIncludedShaders(params string[] names)
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null || graphicsSettings.Length == 0)
            {
                Debug.LogError("[BuildScript] GraphicsSettings.asset not found");
                return;
            }
            var so = new SerializedObject(graphicsSettings[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) { Debug.LogError("[BuildScript] m_AlwaysIncludedShaders property missing"); return; }

            var existing = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var s = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (s != null) existing.Add(s.name);
            }

            foreach (var n in names)
            {
                if (existing.Contains(n)) continue;
                var shader = Shader.Find(n);
                if (shader == null) { Debug.LogWarning($"[BuildScript] shader not found: {n}"); continue; }
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                Debug.Log($"[BuildScript] added to AlwaysIncludedShaders: {n}");
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
