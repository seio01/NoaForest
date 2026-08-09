using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageGroupSO", menuName = "Noa Forest/Stage/Stage Group")]
public class StageGroupSO : ScriptableObject
{
    [SerializeField] private StageSO[] stageData =
        Array.Empty<StageSO>();

    public StageSO[] StageData => stageData;

    public StageSO GetStage(Define.StageId stageId)
    {
        foreach (StageSO stage in stageData)
        {
            if (stage != null && stage.StageId == stageId) return stage;
        }

        return null;
    }

    public bool IsUnlocked(Define.StageId stageId, List<int> clearedStageIds)
    {
        StageSO previousStage = null;

        foreach (StageSO stage in stageData)
        {
            if (stage == null) continue;
            if (stage.StageId == stageId)
            {
                return previousStage == null || ContainsStageId(clearedStageIds, previousStage.StageId);
            }

            previousStage = stage;
        }

        return false;
    }

    private bool ContainsStageId(List<int> stageIds, Define.StageId stageId)
    {
        if (stageIds == null) return false;

        foreach (int savedStageId in stageIds)
        {
            if (savedStageId == (int)stageId) return true;
        }

        return false;
    }
}
