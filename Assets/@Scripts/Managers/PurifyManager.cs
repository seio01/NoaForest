using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PurifyManager
{
    private const string FUNCTION_START_PURIFY = "startPurify";
    private const string FUNCTION_SETTLE_PURIFY = "settlePurify";

    private string _activeRunId;
    private string _activeStageId;

    public string ActiveRunId => _activeRunId;
    public bool HasActiveRun => !string.IsNullOrEmpty(_activeRunId);
    public bool IsStartRequestPending { get; private set; }
    public bool IsSettlementRequestPending { get; private set; }

    public int GetBestFlow(Define.StageId stageId)
    {
        string serverStageId = GetServerStageId(stageId);
        if (serverStageId == null)
            return 0;

        Dictionary<string, int> bestFlows = Managers.Data.CurrentSaveData.BestFlows;
        return bestFlows.TryGetValue(serverStageId, out int bestFlow) ? Mathf.Max(0, bestFlow) : 0;
    }

    public async Task<PurifyStartResponse> StartAsync(Define.StageId stageId)
    {
        if (IsStartRequestPending)
            return null;

        string serverStageId = GetServerStageId(stageId);
        if (serverStageId == null)
        {
            Debug.LogWarning($"[PurifyManager] StageId is invalid: {stageId}");
            return null;
        }

        IsStartRequestPending = true;
        try
        {
            Dictionary<string, object> request = new()
            {
                { "stageId", serverStageId }
            };
            PurifyStartResponse response = await Managers.Data.RequestAsync<PurifyStartResponse>(FUNCTION_START_PURIFY, request);
            if (response == null)
                return null;

            if (!IsValid(response))
            {
                Debug.LogError("[PurifyManager] startPurify response format is invalid.");
                return null;
            }

            _activeRunId = response.RunId;
            _activeStageId = response.StageId;
            return response;
        }
        finally
        {
            IsStartRequestPending = false;
        }
    }

    public async Task<PurifySettlementResponse> SettleAsync(PurifyResultType resultType, int completedFlow, int forestHp)
    {
        if (IsSettlementRequestPending)
            return null;

        string resultTypeValue = GetResultTypeValue(resultType);
        if (!HasActiveRun || resultTypeValue == null || completedFlow < 0 || forestHp < 0)
        {
            Debug.LogWarning("[PurifyManager] Settle request is invalid.");
            return null;
        }

        IsSettlementRequestPending = true;
        try
        {
            Dictionary<string, object> request = new()
            {
                { "runId", _activeRunId },
                { "resultType", resultTypeValue },
                { "completedFlow", completedFlow },
                { "forestHp", forestHp }
            };
            PurifySettlementResponse response = await Managers.Data.SaveAsync<PurifySettlementResponse>(
                FUNCTION_SETTLE_PURIFY,
                request,
                CreateSettlementPatch);
            if (response == null)
                return null;

            if (!IsValid(response))
            {
                Debug.LogError("[PurifyManager] settlePurify response format is invalid.");
                return null;
            }

            _activeRunId = null;
            _activeStageId = null;
            return response;
        }
        finally
        {
            IsSettlementRequestPending = false;
        }
    }

    private string GetServerStageId(Define.StageId stageId)
    {
        int stageNumber = (int)stageId;
        return stageNumber > 0 ? $"stage_{stageNumber:D3}" : null;
    }

    private SavePatch CreateSettlementPatch(PurifySettlementResponse response)
    {
        SavePatch patch = new();
        if (response == null)
            return patch;

        Dictionary<string, int> bestFlows = new(Managers.Data.CurrentSaveData.BestFlows);
        bestFlows[response.StageId] = Mathf.Max(0, response.BestFlow);
        patch.Set(SaveField.BestFlows, bestFlows);

        if (response.ResultType != PurifyResultType.Clear)
            return patch;

        const string STAGE_PREFIX = "stage_";
        if (!response.StageId.StartsWith(STAGE_PREFIX) ||
            !int.TryParse(response.StageId.Substring(STAGE_PREFIX.Length), out int stageId))
            return patch;

        return patch.AddToSet(SaveField.ClearedStageIds, stageId);
    }

    private bool IsValid(PurifyStartResponse response)
    {
        return response != null &&
               !string.IsNullOrWhiteSpace(response.RunId) &&
               !string.IsNullOrWhiteSpace(response.StageId) &&
               response.ConfigVersion > 0 &&
               response.ExpiresAtUtcMillis > 0 &&
               response.AvailableNoaIds != null &&
               response.AvailableNoaIds.Count > 0;
    }

    private bool IsValid(PurifySettlementResponse response)
    {
        if (response == null ||
            !string.Equals(response.RunId, _activeRunId, System.StringComparison.Ordinal) ||
            !string.Equals(response.StageId, _activeStageId, System.StringComparison.Ordinal) ||
            response.ResultType == PurifyResultType.None ||
            response.CompletedFlow < 0 ||
            response.ForestHp < 0 ||
            response.BestFlow < 0 ||
            response.Rewards == null)
            return false;

        foreach (PurifyRewardResponse reward in response.Rewards)
        {
            if (reward == null ||
                reward.Amount <= 0 ||
                !IsSupportedRewardType(reward.RewardType))
                return false;
        }

        return true;
    }

    private bool IsSupportedRewardType(Define.CurrencyType rewardType)
    {
        return rewardType == Define.CurrencyType.Seed ||
               rewardType == Define.CurrencyType.ElementCore ||
               rewardType == Define.CurrencyType.NoaMemory ||
               rewardType == Define.CurrencyType.BlessingTicket;
    }

    private string GetResultTypeValue(PurifyResultType resultType)
    {
        switch (resultType)
        {
            case PurifyResultType.Clear:
                return "clear";
            case PurifyResultType.Fail:
                return "fail";
            default:
                return null;
        }
    }
}
