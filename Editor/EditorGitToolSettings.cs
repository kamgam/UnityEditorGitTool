#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace kamgam.editor.GitTool
{
#if UNITY_2018_4_OR_NEWER
    // Create a new type of Settings Asset.
    class EditorGitToolSettings : ScriptableObject
    {
        public const string SettingsFilePath = "Assets/Editor/EditorGitToolSettings.asset";

        [SerializeField]
        public string GitHashTextAssetPath;

        [SerializeField]
        public bool ShowWarning;

        private static EditorGitToolSettings settings = null;

        internal static EditorGitToolSettings GetOrCreateSettings()
        {
            if (settings != null)
                return settings;

            settings = AssetDatabase.LoadAssetAtPath<EditorGitToolSettings>(SettingsFilePath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EditorGitToolSettings>();
                settings.GitHashTextAssetPath = EditorGitTool.DefaultGitHashFilePath;
                settings.ShowWarning = true;
                // Check if a folder needs to be created
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                    AssetDatabase.CreateFolder("Assets", "Editor");
                AssetDatabase.CreateAsset(settings, SettingsFilePath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class GitToolSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateGitToolSettingsProvider()
        {
            var provider = new SettingsProvider("Project/GitTool", SettingsScope.Project)
            {
                label = "Git Tool",
                guiHandler = (searchContext) =>
                {
                    var settings = EditorGitToolSettings.GetSerializedSettings();
                    settings.Update();

                    // Fix for default toggle not being clickable in some Unity versions (TODO: investigate).
                    var showWarningProp = settings.FindProperty("ShowWarning");
                    showWarningProp.boolValue = EditorGUILayout.ToggleLeft("Show warning", showWarningProp.boolValue);

                    EditorGUILayout.HelpBox("Should a warning be shown before building if there are uncommitted changes?", MessageType.None);
                    EditorGUILayout.PropertyField(settings.FindProperty("GitHashTextAssetPath"), new GUIContent("Hash file path:"));
                    EditorGUILayout.HelpBox("Defines the path where the hash is stored as a text asset file.\n"+
                        "If you want to load this at runtime then store it in Resources, example: 'Assets/Resources/GitHash.asset'.\n"+
                        "\nLoad with :\n" +
                        "  var gitHash = UnityEngine.Resources.Load<TextAsset>(\"GitHash\"); \n"+
                        "  if (gitHash != null)\n"+
                        "  {\n"+
                        "      string versionHash = gitHash.text;\n"+
                        "  }\n", MessageType.None);
                    settings.ApplyModifiedProperties();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting.
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "git", "hash", "build" })
            };

            return provider;
        }
    }
#endif
}
#endif
