using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ButtonClickSoundInstaller
{
    private static readonly string[] _prefabSearchRoots = { "Assets/@Resources", "Assets/Resources/Prefabs" };
    private static readonly string[] _sceneSearchRoots = { "Assets/@Scenes" };

    static ButtonClickSoundInstaller()
    {
        ObjectFactory.componentWasAdded -= HandleComponentAdded;
        ObjectFactory.componentWasAdded += HandleComponentAdded;
    }

    [MenuItem("Tools/UI/Apply Button Click Sounds")]
    public static void ApplyToProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ButtonClickSoundInstaller] Button click sounds cannot be applied during Play Mode.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int changedCount = ApplyToPrefabs();
        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            changedCount += ApplyToScenes();
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ButtonClickSoundInstaller] Button click sound setup completed. Changed: {changedCount}");
    }

    private static void HandleComponentAdded(Component component)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (component is Button button)
        {
            EditorApplication.delayCall += () => EnsureClickSound(button);
            return;
        }

        if (component is ButtonBase buttonBase)
            EditorApplication.delayCall += () => RemoveClickSound(buttonBase.gameObject, true);
    }

    private static void EnsureClickSound(Button button)
    {
        if (!button || EditorApplication.isPlayingOrWillChangePlaymode || PrefabUtility.IsPartOfImmutablePrefab(button)) return;

        GameObject buttonObject = button.gameObject;
        if (buttonObject.GetComponent<ButtonBase>())
        {
            RemoveClickSound(buttonObject, true);
            return;
        }

        if (!buttonObject.GetComponent<UI_ButtonClickSound>()) Undo.AddComponent<UI_ButtonClickSound>(buttonObject);
    }

    private static int ApplyToPrefabs()
    {
        int changedCount = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", _prefabSearchRoots);
        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int prefabChangedCount = ApplyToHierarchy(prefabRoot);
                if (prefabChangedCount <= 0) continue;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                changedCount += prefabChangedCount;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return changedCount;
    }

    private static int ApplyToScenes()
    {
        int changedCount = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", _sceneSearchRoots);
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int sceneChangedCount = 0;
            foreach (GameObject rootObject in scene.GetRootGameObjects()) sceneChangedCount += ApplyToHierarchy(rootObject);
            if (sceneChangedCount <= 0) continue;

            EditorSceneManager.SaveScene(scene);
            changedCount += sceneChangedCount;
        }

        return changedCount;
    }

    private static int ApplyToHierarchy(GameObject rootObject)
    {
        int changedCount = 0;
        Button[] buttons = rootObject.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(button.gameObject)) continue;

            GameObject buttonObject = button.gameObject;
            UI_ButtonClickSound clickSound = buttonObject.GetComponent<UI_ButtonClickSound>();
            if (buttonObject.GetComponent<ButtonBase>())
            {
                if (clickSound && RemoveClickSound(buttonObject, false)) changedCount++;
                continue;
            }

            if (clickSound) continue;

            buttonObject.AddComponent<UI_ButtonClickSound>();
            changedCount++;
        }

        return changedCount;
    }

    private static bool RemoveClickSound(GameObject buttonObject, bool recordUndo)
    {
        if (!buttonObject) return false;

        UI_ButtonClickSound clickSound = buttonObject.GetComponent<UI_ButtonClickSound>();
        if (!clickSound) return false;

        if (recordUndo) Undo.DestroyObjectImmediate(clickSound);
        else UnityEngine.Object.DestroyImmediate(clickSound);
        return true;
    }
}
