using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PopupGenerator : EditorWindow
{
    private const string MENU_PATH = "Tools/Popup Generator";
    private const string POPUP_TEMPLATE_PATH =
        "Assets/Resources/Prefabs/Popups/UI_PopupBase.prefab";
    private const string POPUP_PREFAB_ROOT_PATH = "Assets/Resources/Prefabs/Popups";
    private const string POPUP_SCRIPT_ROOT_PATH = "Assets/@Scripts/Contents/Popups";
    private const string PENDING_GENERATION_KEY = "PopupGenerator.PendingGeneration";

    private static readonly HashSet<string> _csharpKeywords = new()
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default",
        "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
        "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte",
        "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        "add", "alias", "and", "ascending", "async", "await", "by", "descending",
        "dynamic", "equals", "from", "get", "global", "group", "init", "into", "join",
        "let", "managed", "nameof", "nint", "not", "notnull", "nuint", "on", "or",
        "orderby", "partial", "record", "remove", "required", "select", "set", "unmanaged",
        "value", "var", "when", "where", "with", "yield"
    };

    private string _popupName = "UI_NewPopup";
    private bool _createSubfolder;
    private string _subfolderName = string.Empty;
    private bool _createPopupScript = true;

    static PopupGenerator()
    {
        EditorApplication.delayCall += TryCompletePendingGeneration;
    }

    [MenuItem(MENU_PATH)]
    private static void OpenWindow()
    {
        PopupGenerator window = GetWindow<PopupGenerator>();
        window.titleContent = new GUIContent("Popup Generator");
        window.minSize = new Vector2(420f, 300f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Popup Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "UI_PopupBase를 복제한 뒤 실제 팝업 루트에 PopupBase 또는 전용 팝업 스크립트를 부착합니다.",
            MessageType.Info);
        EditorGUILayout.Space();

        _popupName = EditorGUILayout.TextField("Popup Name", _popupName).Trim();
        _createSubfolder = EditorGUILayout.Toggle("Create Subfolder", _createSubfolder);

        if (_createSubfolder)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _subfolderName = EditorGUILayout.TextField("Subfolder Name", _subfolderName).Trim();
            }
        }

        _createPopupScript = EditorGUILayout.Toggle("Create Popup Script", _createPopupScript);

        string prefabFolderPath = GetTargetFolderPath(POPUP_PREFAB_ROOT_PATH);
        string scriptFolderPath = GetTargetFolderPath(POPUP_SCRIPT_ROOT_PATH);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefab", $"{prefabFolderPath}/{_popupName}.prefab");

        if (_createPopupScript)
        {
            EditorGUILayout.LabelField("Script", $"{scriptFolderPath}/{_popupName}.cs");
            EditorGUILayout.LabelField("Component", $"{_popupName} : PopupBase");
        }
        else
        {
            EditorGUILayout.LabelField("Component", nameof(PopupBase));
        }

        EditorGUILayout.Space();

        string pendingGenerationJson = SessionState.GetString(PENDING_GENERATION_KEY, string.Empty);
        string validationError = GetValidationError();

        if (!string.IsNullOrEmpty(pendingGenerationJson))
        {
            EditorGUILayout.HelpBox(
                "팝업 스크립트 컴파일 후 프리팹 생성을 완료하는 중입니다.",
                MessageType.Info);

            if (GUILayout.Button("Cancel Pending Generation"))
            {
                SessionState.EraseString(PENDING_GENERATION_KEY);
                Debug.LogWarning(
                    "[PopupGenerator] Pending popup generation was canceled. " +
                    "The generated script was retained.");
            }
        }
        else if (!string.IsNullOrEmpty(validationError))
        {
            EditorGUILayout.HelpBox(validationError, MessageType.Error);
        }

        bool cannotGenerate =
            EditorApplication.isPlaying ||
            EditorApplication.isCompiling ||
            !string.IsNullOrEmpty(pendingGenerationJson) ||
            !string.IsNullOrEmpty(validationError);

        using (new EditorGUI.DisabledScope(cannotGenerate))
        {
            if (GUILayout.Button("Generate Popup", GUILayout.Height(36f)))
            {
                GeneratePopup();
            }
        }
    }

    private void GeneratePopup()
    {
        string prefabFolderPath = GetTargetFolderPath(POPUP_PREFAB_ROOT_PATH);
        string scriptFolderPath = GetTargetFolderPath(POPUP_SCRIPT_ROOT_PATH);

        try
        {
            EnsureFolderExists(prefabFolderPath);

            if (!_createPopupScript)
            {
                string prefabPath = $"{prefabFolderPath}/{_popupName}.prefab";
                CreatePopupPrefab(_popupName, prefabPath, typeof(PopupBase));
                OpenCreatedPopup(prefabPath);
                Debug.Log($"[PopupGenerator] Popup generated: {prefabPath}");
                return;
            }

            EnsureFolderExists(scriptFolderPath);

            string scriptPath = $"{scriptFolderPath}/{_popupName}.cs";
            string prefabPathWithScript = $"{prefabFolderPath}/{_popupName}.prefab";
            PendingPopupGeneration pendingGeneration = new()
            {
                PopupName = _popupName,
                PrefabPath = prefabPathWithScript,
                ScriptPath = scriptPath
            };

            SessionState.SetString(
                PENDING_GENERATION_KEY,
                JsonUtility.ToJson(pendingGeneration));

            File.WriteAllText(scriptPath, CreatePopupScriptContent(_popupName));
            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log($"[PopupGenerator] Popup script generated. Waiting for compilation: {scriptPath}");
        }
        catch (Exception exception)
        {
            SessionState.EraseString(PENDING_GENERATION_KEY);
            Debug.LogError($"[PopupGenerator] Popup generation failed.\n{exception}");
        }
    }

    private string GetValidationError()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(POPUP_TEMPLATE_PATH) == null)
        {
            return $"Popup template을 찾을 수 없습니다: {POPUP_TEMPLATE_PATH}";
        }

        if (string.IsNullOrWhiteSpace(_popupName))
        {
            return "Popup Name을 입력해 주세요.";
        }

        if (HasInvalidFileNameCharacter(_popupName))
        {
            return "Popup Name에 파일명으로 사용할 수 없는 문자가 포함되어 있습니다.";
        }

        if (_createPopupScript && !IsValidCSharpIdentifier(_popupName))
        {
            return "스크립트를 생성하려면 Popup Name이 유효한 C# 클래스 이름이어야 합니다.";
        }

        if (_createSubfolder && !IsValidFolderName(_subfolderName))
        {
            return "유효한 Subfolder Name을 입력해 주세요. 한 단계의 폴더 이름만 사용할 수 있습니다.";
        }

        if (FindPopupPrefabPath(_popupName) != null)
        {
            return $"동일한 이름의 팝업 프리팹이 이미 존재합니다: {_popupName}";
        }

        string prefabPath = $"{GetTargetFolderPath(POPUP_PREFAB_ROOT_PATH)}/{_popupName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return $"대상 경로에 팝업 프리팹이 이미 존재합니다: {prefabPath}";
        }

        if (!_createPopupScript)
        {
            return string.Empty;
        }

        string scriptPath = $"{GetTargetFolderPath(POPUP_SCRIPT_ROOT_PATH)}/{_popupName}.cs";
        if (AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath) != null || File.Exists(scriptPath))
        {
            return $"대상 경로에 팝업 스크립트가 이미 존재합니다: {scriptPath}";
        }

        if (FindScriptPath(_popupName) != null)
        {
            return $"동일한 이름의 스크립트가 이미 존재합니다: {_popupName}";
        }

        if (FindType(_popupName) != null)
        {
            return $"동일한 이름의 C# 타입이 이미 존재합니다: {_popupName}";
        }

        return string.Empty;
    }

    private string GetTargetFolderPath(string rootPath)
    {
        return _createSubfolder
            ? $"{rootPath}/{_subfolderName}"
            : rootPath;
    }

    private static void TryCompletePendingGeneration()
    {
        string pendingGenerationJson = SessionState.GetString(PENDING_GENERATION_KEY, string.Empty);
        if (string.IsNullOrEmpty(pendingGenerationJson))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryCompletePendingGeneration;
            return;
        }

        PendingPopupGeneration pendingGeneration =
            JsonUtility.FromJson<PendingPopupGeneration>(pendingGenerationJson);

        if (pendingGeneration == null)
        {
            SessionState.EraseString(PENDING_GENERATION_KEY);
            Debug.LogError("[PopupGenerator] Pending popup generation data is invalid.");
            return;
        }

        MonoScript popupScript = AssetDatabase.LoadAssetAtPath<MonoScript>(pendingGeneration.ScriptPath);
        if (popupScript == null)
        {
            SessionState.EraseString(PENDING_GENERATION_KEY);
            Debug.LogError(
                $"[PopupGenerator] 생성된 팝업 스크립트를 찾을 수 없습니다: " +
                pendingGeneration.ScriptPath);
            return;
        }

        Type popupType = popupScript != null ? popupScript.GetClass() : null;

        if (popupType == null)
        {
            Debug.LogError(
                $"[PopupGenerator] 생성된 팝업 타입을 찾을 수 없습니다. " +
                $"컴파일 오류를 확인해 주세요: {pendingGeneration.ScriptPath}");
            return;
        }

        if (!typeof(PopupBase).IsAssignableFrom(popupType) || popupType.IsAbstract)
        {
            SessionState.EraseString(PENDING_GENERATION_KEY);
            Debug.LogError(
                $"[PopupGenerator] {popupType.Name} 타입은 생성 가능한 PopupBase 파생 타입이 아닙니다.");
            return;
        }

        try
        {
            CreatePopupPrefab(pendingGeneration.PopupName, pendingGeneration.PrefabPath, popupType);
            SessionState.EraseString(PENDING_GENERATION_KEY);
            OpenCreatedPopup(pendingGeneration.PrefabPath);
            Debug.Log($"[PopupGenerator] Popup generated: {pendingGeneration.PrefabPath}");
        }
        catch (Exception exception)
        {
            SessionState.EraseString(PENDING_GENERATION_KEY);
            Debug.LogError($"[PopupGenerator] Popup prefab generation failed.\n{exception}");
        }
    }

    private static void CreatePopupPrefab(string popupName, string prefabPath, Type componentType)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            throw new InvalidOperationException($"Popup prefab already exists: {prefabPath}");
        }

        if (!AssetDatabase.CopyAsset(POPUP_TEMPLATE_PATH, prefabPath))
        {
            throw new InvalidOperationException($"Popup template copy failed: {prefabPath}");
        }

        GameObject popupRoot = null;

        try
        {
            popupRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            popupRoot.name = popupName;

            PopupBase[] existingPopupComponents = popupRoot.GetComponents<PopupBase>();
            foreach (PopupBase existingPopupComponent in existingPopupComponents)
            {
                DestroyImmediate(existingPopupComponent);
            }

            popupRoot.AddComponent(componentType);
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(popupRoot, prefabPath);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException($"Popup prefab save failed: {prefabPath}");
            }
        }
        catch
        {
            AssetDatabase.DeleteAsset(prefabPath);
            throw;
        }
        finally
        {
            if (popupRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(popupRoot);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static string CreatePopupScriptContent(string popupName)
    {
        return $@"using UnityEngine;
using UnityEngine.UI;

public class {popupName} : PopupBase
{{
        
}}
";
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] pathParts = folderPath.Split('/');
        string currentPath = pathParts[0];

        for (int index = 1; index < pathParts.Length; index++)
        {
            string nextPath = $"{currentPath}/{pathParts[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                string createdFolderGuid = AssetDatabase.CreateFolder(currentPath, pathParts[index]);
                if (string.IsNullOrEmpty(createdFolderGuid))
                {
                    throw new IOException($"Folder creation failed: {nextPath}");
                }
            }

            currentPath = nextPath;
        }
    }

    private static string FindPopupPrefabPath(string popupName)
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            $"{popupName} t:Prefab",
            new[] { POPUP_PREFAB_ROOT_PATH });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(prefabPath),
                    popupName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return prefabPath;
            }
        }

        return null;
    }

    private static string FindScriptPath(string scriptName)
    {
        string[] scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:Script", new[] { "Assets" });

        foreach (string scriptGuid in scriptGuids)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(scriptPath),
                    scriptName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return scriptPath;
            }
        }

        return null;
    }

    private static Type FindType(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static bool IsValidCSharpIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value) || _csharpKeywords.Contains(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (!(char.IsLetterOrDigit(value[index]) || value[index] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidFolderName(string folderName)
    {
        return !string.IsNullOrWhiteSpace(folderName) &&
               folderName != "." &&
               folderName != ".." &&
               !folderName.Contains("/") &&
               !folderName.Contains("\\") &&
               !HasInvalidFileNameCharacter(folderName);
    }

    private static bool HasInvalidFileNameCharacter(string value)
    {
        return value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
    }

    private static void OpenCreatedPopup(string assetPath)
    {
        UnityEngine.Object createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (createdAsset == null)
        {
            Debug.LogError($"[PopupGenerator] Created popup asset not found: {assetPath}");
            return;
        }

        Selection.activeObject = createdAsset;
        EditorGUIUtility.PingObject(createdAsset);

        if (!AssetDatabase.OpenAsset(createdAsset))
        {
            Debug.LogWarning($"[PopupGenerator] Created popup could not be opened: {assetPath}");
        }
    }

    [Serializable]
    private class PendingPopupGeneration
    {
        public string PopupName;
        public string PrefabPath;
        public string ScriptPath;
    }
}
