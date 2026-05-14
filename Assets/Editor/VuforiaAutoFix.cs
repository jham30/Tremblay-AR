using System.IO;
using UnityEditor;
using UnityEngine;

// Deletes stale Vuforia build artifacts once per Editor session on startup.
// Fixes DRIVER_CONFIG_LOAD_ERROR caused by interrupted shutdown on Windows.
[InitializeOnLoad]
public static class VuforiaAutoFix
{
    const string SessionKey = "VuforiaAutoFix.Cleaned";

    static VuforiaAutoFix()
    {
        // Only run once per Unity Editor session, not on every domain reload.
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        string[] foldersToClean = {
            Path.Combine(projectPath, "Library", "Bee"),
            Path.Combine(projectPath, "obj"),
            Path.Combine(projectPath, "Temp"),
        };

        bool cleaned = false;
        foreach (string folder in foldersToClean)
        {
            if (Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, true);
                    cleaned = true;
                    Debug.Log($"[VuforiaAutoFix] Deleted: {folder}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[VuforiaAutoFix] Could not delete {folder}: {e.Message}");
                }
            }
        }

        if (cleaned)
            Debug.Log("[VuforiaAutoFix] Vuforia build artifacts cleaned. Unity will recompile scripts.");
    }
}
