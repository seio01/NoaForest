using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PurifyScene))]
public class PurifySceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("노아 테스트 소환", EditorStyles.boldLabel);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Play Mode에서만 테스트 소환을 사용할 수 있습니다.", MessageType.Info);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            DrawTierButtons(Define.NoaTier.Tier1);
            DrawTierButtons(Define.NoaTier.Tier2);
            DrawTierButtons(Define.NoaTier.Tier3);
        }
    }

    private void DrawTierButtons(Define.NoaTier tier)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{(int)tier}단계", GUILayout.Width(45f));
        DrawSummonButton("물", Define.ElementType.Water, tier);
        DrawSummonButton("불", Define.ElementType.Fire, tier);
        DrawSummonButton("바람", Define.ElementType.Wind, tier);
        DrawSummonButton("땅", Define.ElementType.Earth, tier);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSummonButton(string label, Define.ElementType element, Define.NoaTier tier)
    {
        if (!GUILayout.Button(label, GUILayout.Height(24f))) return;

        PurifyScene purifyScene = (PurifyScene)target;
        purifyScene.TrySummonNoaForTest(element, tier);
    }
}
