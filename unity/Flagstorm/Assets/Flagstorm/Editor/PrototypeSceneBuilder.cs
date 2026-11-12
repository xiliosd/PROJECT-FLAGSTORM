using Flagstorm.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Flagstorm.EditorTools
{
    // Genera la escena de prototipo y sus recursos desde código para que se puedan reconstruir sin pasos manuales.
    // Uso en lote: Unity.exe -batchmode -quit -projectPath unity/Flagstorm
    //   -executeMethod Flagstorm.EditorTools.PrototypeSceneBuilder.Build
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Flagstorm/Scenes/PrototypeHolo.unity";
        private const string MaterialPath = "Assets/Flagstorm/Materials/HoloUnlit.mat";
        private const string PanelSettingsPath = "Assets/Flagstorm/UI/HoloPanelSettings.asset";
        private const string ThemePath = "Assets/Flagstorm/UI/HoloTheme.tss";

        [MenuItem("Flagstorm/Reconstruir escena de prototipo")]
        public static void Build()
        {
            var material = CreateMaterial();
            var panelSettings = CreatePanelSettings();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = HoloPalette.Ocean;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 60f, -10f), Quaternion.Euler(80f, 0f, 0f));

            var matchObject = new GameObject("Partida");
            matchObject.AddComponent<UIDocument>().panelSettings = panelSettings;
            matchObject.AddComponent<CellPicker>();
            var controller = matchObject.AddComponent<MatchController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("worldCamera").objectReferenceValue = camera;
            serialized.FindProperty("cellMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            // El prototipo se juega con el teléfono en vertical (spec: maquetas aprobadas).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Flagstorm] Escena generada en {ScenePath}");
        }

        private static Material CreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            // Doble cara: las mallas son planas y así no desaparecen si cambia la inclinación de la cámara.
            material.SetFloat("_Cull", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PanelSettings CreatePanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            return settings;
        }
    }
}
