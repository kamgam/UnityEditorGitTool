#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace kamgam.editor.GitTool
{
    #region Settings

#if UNITY_2018_4_OR_NEWER
    // Create a new type of Settings Asset.
    class GitToolSettings : ScriptableObject
    {
        public const string SettingsFilePath = "Assets/Editor/GitToolSettings.asset";

        [SerializeField]
        public string GitHashTextAssetPath;

        [SerializeField]
        public bool ShowWarning;

        internal static GitToolSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GitToolSettings>(SettingsFilePath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GitToolSettings>();
                settings.GitHashTextAssetPath = EditorGitTool.DefaultGitHashFilePath;
                settings.ShowWarning = true;
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
                    var settings = GitToolSettings.GetSerializedSettings();
                    EditorGUILayout.PropertyField(settings.FindProperty("ShowWarning"), new GUIContent("Show warning:"));
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
    #endregion

    /// <summary>
    /// Saves the git hash into a text asset.
    /// </summary>
    public static class EditorGitTool
    {
        public const string DefaultGitHashFilePath = "Assets/Resources/GitHash.asset";

#if !UNITY_2018_4_OR_NEWER
        private static bool prefsLoaded = false;
        public static string GitHashFilePath = DefaultGitHashFilePath;
        public static bool ShowWarning = true;

        [PreferenceItem("Git Tool")]
        private static void CustomPreferencesGUI()
        {
            if (!prefsLoaded)
            {
                GitHashFilePath = EditorPrefs.GetString("kamgam.EditorGitTools.DefaultGitHashFilePath", GitHashFilePath);
                ShowWarning = EditorPrefs.GetBool("kamgam.EditorGitTools.ShowWarning", ShowWarning);
                prefsLoaded = true;
            }

            GitHashFilePath = EditorGUILayout.TextField(GitHashFilePath);
            ShowWarning = EditorGUILayout.Toggle("Show Warning: ", ShowWarning);

            if (GUI.changed)
            {
                EditorPrefs.SetString("kamgam.EditorGitTools.DefaultGitHashFilePath", GitHashFilePath);
                EditorPrefs.SetBool("kamgam.EditorGitTools.ShowWarning", ShowWarning);
            }
        }
#endif

        /// <summary>
        /// Update the hash from the menu.
        /// </summary>
        [MenuItem("Tools/Git/SaveHash")]
        public static void SaveHashFromMenu()
        {
            SaveHash();
        }

        /// <summary>
        /// Fetch the hash from git, add postFix to it and then save it in gitHashFilePath.
        /// </summary>
        /// <param name="postFix">Text to be appended to the hash.</param>
        /// <param name="gitHashFilePath">Git hashfile path, will use the path set the settings if not specified. Example: "Assets/Resources/GitHash.asset"</param>
        public static void SaveHash(string postFix = "", string gitHashFilePath = null)
        {
            if(string.IsNullOrEmpty(gitHashFilePath))
            {
#if UNITY_2018_4_OR_NEWER
                gitHashFilePath = GitToolSettings.GetOrCreateSettings().GitHashTextAssetPath;
#else
                gitHashFilePath = GitHashFilePath;
#endif
            }

            Debug.Log("GitTools: PreExport() - writing git hash into '" + gitHashFilePath + "'");

            string gitHash = ExecAndReadFirstLine("git rev-parse --short HEAD");
            if (gitHash == null)
            {
                Debug.LogError("GitTools: not git hash found!");
                gitHash = "unknown";
            }

            Debug.Log("GitTools: git hash is '" + gitHash + "'");

            AssetDatabase.DeleteAsset(gitHashFilePath);
            var text = new TextAsset(gitHash + postFix);
            AssetDatabase.CreateAsset(text, gitHashFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Counts modified, new or simply unknown files in working tree.
        /// </summary>
        /// <returns></returns>
        public static int CountChanges()
        {
            Debug.Log("GitTools: CountChanges() - counts modified, new or simply unknown files in working tree.");

            string statusResult = Exec("git status --porcelain");
            if (statusResult == null)
            {
                return 0;
            }

            return countLines(statusResult);
        }

        private static int countLines(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (str == string.Empty)
                return 0;
            int index = -1;
            int count = 0;
            while ( (index = str.IndexOf(Environment.NewLine, index + 1)) != -1 )
            {
                count++;
            }

            return count + 1;
        }
		
        public static string ExecAndReadFirstLine(string command, int maxWaitTimeInSec = 5)
        {
            string result = Exec(command, maxWaitTimeInSec);

            // first line only
            if (result != null)
            {
                int i = result.IndexOf("\n");
                if (i > 0)
                {
                    result = result.Substring(0, i);
                }
            }

            return result;
        }

        public static string Exec(string command, int maxWaitTimeInSec = 5)
        {
            try
            {
#if UNITY_EDITOR_WIN
                string shellCmd = "cmd.exe";
                string shellCmdArg = "/c";
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
		string shellCmd = "bash";
                string shellCmdArg = "-c";
#else
		string shellCmd = "";
		string shellCmdArg = "";
#endif

                string cmdArguments = shellCmdArg + " \"" + command + "\"";
                Debug.Log("GitTool.Exec: Attempting to execute command: " + (shellCmd + " " + cmdArguments));
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(shellCmd, cmdArguments);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;

                // Debug.Log("GitTool.Exec: Running process...");
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                proc.WaitForExit(maxWaitTimeInSec * 1000);
                string result = proc.StandardOutput.ReadToEnd();

                Debug.Log("GitTool.Exec: done");
                return result;
            }
            catch (System.Exception e)
            {
                Debug.Log("GitTool.Exec Error: " + e);
                return null;
            }
        }
    }

    /// <summary>
    /// Hooks up to the BuildProcess and calls Git.
    /// </summary>
    class GitBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // show warning
#if UNITY_2018_4_OR_NEWER
            bool showWarning = GitToolSettings.GetOrCreateSettings().ShowWarning;
#else
            bool showWarning = EditorGitTool.ShowWarning;
#endif

            bool commitsPending = false;
            if(EditorGitTool.CountChanges() > 0 && showWarning)
            {
                commitsPending = true;
                var continueWithoutCommit = EditorUtility.DisplayDialog(
                    "GIT: Commit your changes!",
                    "There are still uncommitted changes.\nDo you want to proceed with the build?",
                    "Build Anyway", "Cancel Build"
                );
                if (continueWithoutCommit == false)
                {
                    throw new Exception("User canceled build because there are still uncommitted changes.");
                }
            }

            // Export git hash to text asset for runtime use.
            // Add a "+" to the hash to indicate that this was built without commiting pending changes.
            EditorGitTool.SaveHash(commitsPending ? "+" : "");
        }
    }
}
#endif
