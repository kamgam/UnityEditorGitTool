# UnityEditorGitTool
A tool which automatically saves the git hash into a text asset before build. Useful if you want to use the hash at runtime. For example to display it in addition to the current version of the game.

## Requirements
* You need to have git installed ("git" command in PATH). To test simply open your commandline, type in "git" and see what happens.
* Your project must use git (obviously).
* Unity 2017+ (may work with earlier versions too, have not tested it there)

## Usage

At build time it will try to fetch the current hash from git and save it in a text asset (default: "Assets/Resources/GitHash.asset").

It can show a warning before the build if there are uncommited changes (enabled by default). This can be disabled in the settings.

![Alt Warning Dialog](documentation/warning.png?raw=true "Warning dialog at build time.")

You can also trigger saving the hash manually via Menu > Tools > Git > SaveHash

![Alt Menu](documentation/menu.png?raw=true "Save hash manually.")

The location of the hash file and whether or not a warning should be displayed can be configured in the settings. If Unity is version 2018.4 or newer then these settings are stored in an asset ("Assets/Editor/GitToolSettings.asset").

![Alt Settings 2018.4+](documentation/settings.png?raw=true "Settings")

Older versions of Unity (pre 2018.4) use the EditorPrefs ("kamgam.EditorGitTools.*").

![Alt Settings < 2018.4](documentation/prefs.png?raw=true "Settings")

### Use at runtime
Here is an example of how to read the hash at runtime. Notice that the path depends on your settings. This example assumes you are using the default path "Assets/Resources/GitHash.asset".
```csharp
public static string _versionHash = null;
public static string GetVersionHash()
{
    if (_versionHash == null)
    {
        _versionHash = "unknown";
        var gitHash = UnityEngine.Resources.Load<TextAsset>("GitHash");
        if (gitHash != null)
        {
            _versionHash = gitHash.text;
        }
    }
    return _versionHash;
}
```