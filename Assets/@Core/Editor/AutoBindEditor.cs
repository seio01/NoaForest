using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(UI_Base), true)]
public class AutoBindEditor : OdinEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(5);

        if(GUILayout.Button("Bind"))
        {
            AutoBind((MonoBehaviour)target);
        }
    }

    private void AutoBind(MonoBehaviour targetBehaviour)
    {
        var serializedObject = new SerializedObject(targetBehaviour);
        serializedObject.Update();

        var fields = targetBehaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        var nameIndex = BuildSceneNameIndex(targetBehaviour);

        foreach (var field in fields)
        {
            if (!(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null))
                continue;

            if (!(typeof(Component).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(GameObject)))
                continue;

            var property = serializedObject.FindProperty(field.Name);
            if (property == null)
            {
                Debug.LogError($"[AutoBindEditor] Serialized property is missing: {field.Name}");
                continue;
            }

            if (TryBindSameGameObject(targetBehaviour, field, property))
            {
                continue;
            }

            string objectName = field.Name;
            if (!nameIndex.TryGetValue(objectName, out var nameMatches) ||
                nameMatches.Count == 0)
            {
                Debug.LogError($"[AutoBindEditor] {field.Name} not found in {targetBehaviour.name}");
                continue;
            }

            if (field.FieldType == typeof(GameObject))
            {
                if (nameMatches.Count > 1)
                {
                    LogDuplicateNameWarning(targetBehaviour.transform, objectName, nameMatches);
                    continue;
                }

                property.objectReferenceValue = nameMatches[0].gameObject;
            }
            else
            {
                var componentMatches = new List<Component>();
                for (int i = 0; i < nameMatches.Count; i++)
                {
                    var c = nameMatches[i].GetComponent(field.FieldType);
                    if (c != null) componentMatches.Add(c);
                }

                if (componentMatches.Count == 0)
                {
                    Debug.Log($"[AutoBindEditor] {field.Name} (Component of type {field.FieldType.Name}) not found in {targetBehaviour.name}");
                    continue;
                }

                if (componentMatches.Count > 1)
                {
                    var transforms = componentMatches.Select(c => c.transform).ToList();
                    LogDuplicateNameWarning(targetBehaviour.transform, objectName, transforms, field.FieldType);
                    continue;
                }

                property.objectReferenceValue = componentMatches[0];
            }
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[AutoBindEditor] Auto-bound components for {targetBehaviour.name}");
    }

    private bool TryBindSameGameObject(
        MonoBehaviour targetBehaviour,
        FieldInfo field,
        SerializedProperty property)
    {
        if (!typeof(Component).IsAssignableFrom(field.FieldType) ||
            !string.Equals(
                field.Name,
                field.FieldType.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Component component = targetBehaviour.GetComponent(field.FieldType);
        if (!component)
        {
            return false;
        }

        property.objectReferenceValue = component;
        return true;
    }

    private Dictionary<string, List<Transform>> BuildSceneNameIndex(
        MonoBehaviour targetBehaviour)
    {
        var dict = new Dictionary<string, List<Transform>>(StringComparer.OrdinalIgnoreCase);

        Scene scene = targetBehaviour.gameObject.scene;
        if (!scene.IsValid())
        {
            CollectTransformHierarchy(targetBehaviour.transform.root, dict);
            return dict;
        }

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            CollectTransformHierarchy(rootObject.transform, dict);
        }

        return dict;
    }

    private void CollectTransformHierarchy(
        Transform current,
        Dictionary<string, List<Transform>> dict)
    {
        if (!dict.TryGetValue(current.name, out var list))
        {
            list = new List<Transform>();
            dict[current.name] = list;
        }

        list.Add(current);

        foreach (Transform child in current)
        {
            CollectTransformHierarchy(child, dict);
        }
    }

    private void LogDuplicateNameWarning(Transform root, string name, List<Transform> matches, System.Type componentType = null)
    {
        string typeLabel = componentType == null ? "GameObject" : $"Component({componentType.Name})";
        string paths = string.Join(", ", matches.Select(t => GetRelativePath(root, t)));
        Debug.Log($"[AutoBindEditor] Duplicate name '{name}' detected for {typeLabel} under '{root.name}'. Skipping bind for this field. Matches: {paths}");
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return target != null ? target.name : "<null>";
        }

        var parts = new List<string>();
        var current = target;
        while (current != null)
        {
            parts.Add(current.name);
            if (current == root)
                break;
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

}
