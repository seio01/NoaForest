using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ComponentPrefabCreatorWindow : EditorWindow
{
    private const string COMPONENT_PREFAB_FOLDER = "Assets/@Resources/Components";
    private readonly List<GameObject> _componentPrefabs = new();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Component Creator")]
    private static void OpenWindow()
    {
        ComponentPrefabCreatorWindow window = GetWindow<ComponentPrefabCreatorWindow>();
        window.titleContent = new GUIContent("Component Creator");
        window.minSize = new Vector2(260f, 180f);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshPrefabList();
    }

    private void OnProjectChange()
    {
        RefreshPrefabList();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Component Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(COMPONENT_PREFAB_FOLDER, EditorStyles.miniLabel);
        EditorGUILayout.Space();

        if (_componentPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("Component prefab을 찾을 수 없습니다.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        using (new EditorGUI.DisabledScope(
                   EditorApplication.isPlaying ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating))
        {
            foreach (GameObject componentPrefab in _componentPrefabs)
            {
                if (componentPrefab == null)
                {
                    continue;
                }

                if (GUILayout.Button(componentPrefab.name, GUILayout.Height(28f)))
                {
                    CreatePrefabInstance(componentPrefab);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshPrefabList()
    {
        _componentPrefabs.Clear();

        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { COMPONENT_PREFAB_FOLDER });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject componentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (componentPrefab != null)
            {
                _componentPrefabs.Add(componentPrefab);
            }
        }

        _componentPrefabs.Sort(
            (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
    }

    private static void CreatePrefabInstance(GameObject componentPrefab)
    {
        Transform targetParent = FindTargetParent();
        if (targetParent == null)
        {
            return;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(componentPrefab, targetParent) as GameObject;

        if (instance == null)
        {
            Debug.LogError($"[ComponentPrefabCreator] Prefab 생성 실패: {componentPrefab.name}");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Create {componentPrefab.name}");
        instance.transform.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(instance.scene);

        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(instance);
    }

    private static Transform FindTargetParent()
    {
        Transform selectedTransform = Selection.activeTransform;
        if (selectedTransform)
        {
            if (EditorUtility.IsPersistent(selectedTransform))
            {
                Debug.LogError("[ComponentPrefabCreator] Select a Hierarchy object instead of a Project asset.");
                return null;
            }

            StageHandle currentStage = StageUtility.GetCurrentStageHandle();
            if (StageUtility.GetStageHandle(selectedTransform.gameObject) != currentStage)
            {
                Debug.LogError("[ComponentPrefabCreator] The selected object is not in the current stage.");
                return null;
            }

            if (!selectedTransform.TryGetComponent(out RectTransform _))
            {
                Debug.LogError("[ComponentPrefabCreator] The selected parent requires a RectTransform.");
                return null;
            }

            return selectedTransform;
        }

        Canvas rootCanvas = FindRootCanvas();
        if (rootCanvas)
        {
            return rootCanvas.transform;
        }

        Debug.LogError("[ComponentPrefabCreator] Select a UI parent or add a root Canvas to the current stage.");
        return null;
    }

    private static Canvas FindRootCanvas()
    {
        StageHandle currentStage = StageUtility.GetCurrentStageHandle();
        Canvas[] canvases = currentStage.FindComponentsOfType<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (IsValidRootCanvas(canvas, currentStage))
            {
                return canvas;
            }
        }

        return null;
    }

    private static bool IsValidRootCanvas(Canvas canvas, StageHandle currentStage)
    {
        if (canvas == null || !canvas.gameObject.activeInHierarchy || !canvas.isRootCanvas)
        {
            return false;
        }

        if (EditorUtility.IsPersistent(canvas) ||
            (canvas.hideFlags & HideFlags.HideInHierarchy) != 0)
        {
            return false;
        }

        return StageUtility.GetStageHandle(canvas.gameObject) == currentStage;
    }
}
