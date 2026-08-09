using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 에디터에서 첫번째 씬에서 시작, 현재 씬에서 시작 기능을 담당.
/// </summary>
public class EditorPlay : Editor
{
    
    [MenuItem("Play/First Scene %0")]
    public static void PlayFirstScene()
    {
        var firstScene = EditorBuildSettings.scenes[0];
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScene.path);

        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.isPlaying = true;
    }
    
    [MenuItem("Play/Current Scene %1")]
    public static void PlayCurrentScene()
    {
        EditorSceneManager.playModeStartScene = null;
        EditorApplication.isPlaying = true;
    }
}