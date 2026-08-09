using System;
using System.Collections.Generic;

[Serializable]
public class PurifyStartResponse
{
    public string RunId;
    public string StageId;
    public int ConfigVersion;
    public long ExpiresAtUtcMillis;
    public List<string> AvailableNoaIds;
}

[Serializable]
public class PurifyRewardResponse
{
    public Define.CurrencyType RewardType;
    public int Amount;
}

[Serializable]
public class PurifySettlementResponse
{
    public string RunId;
    public string StageId;
    public PurifyResultType ResultType;
    public int CompletedFlow;    
    public int ForestHp;
    public bool IsNewBest;
    public int BestFlow;
    public bool IsFirstClear;
    public List<PurifyRewardResponse> Rewards;
}
