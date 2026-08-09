using System;
using System.Collections.Generic;

public class RewardPresentationItem
{
    public Define.CurrencyType CurrencyType { get; }
    public int Amount { get; }

    public RewardPresentationItem(Define.CurrencyType currencyType, int amount)
    {
        CurrencyType = currencyType;
        Amount = amount;
    }
}

public class RewardPresentationBatch
{
    public string BatchId { get; }
    public Define.Scene DestinationScene { get; }
    public List<RewardPresentationItem> Items { get; }

    public RewardPresentationBatch(string batchId, Define.Scene destinationScene, List<RewardPresentationItem> items)
    {
        BatchId = batchId;
        DestinationScene = destinationScene;
        Items = items;
    }
}

public class RewardManager
{
    private readonly List<RewardPresentationBatch> _pendingBatches = new();
    private readonly HashSet<string> _registeredBatchIds = new();

    public event Action<Define.Scene> PendingRewardAdded;

    //재화 한개 용도
    public void Enqueue(Define.CurrencyType currencyType, Define.Scene destinationScene, int amount)
    {
        EnqueueBatch(Guid.NewGuid().ToString("N"), destinationScene, new List<RewardPresentationItem>
        {
            new RewardPresentationItem(currencyType, amount)
        });
    }

    public void EnqueueBatch(string batchId, Define.Scene destinationScene, List<RewardPresentationItem> items)
    {
        if (string.IsNullOrEmpty(batchId) || items == null || _registeredBatchIds.Contains(batchId)) return;

        List<RewardPresentationItem> validItems = new();
        foreach (RewardPresentationItem item in items)
        {
            if (item != null && item.Amount > 0) validItems.Add(item);
        }

        if (validItems.Count == 0) return;

        _registeredBatchIds.Add(batchId);
        _pendingBatches.Add(new RewardPresentationBatch(batchId, destinationScene, validItems));
        PendingRewardAdded?.Invoke(destinationScene);
    }

    public List<RewardPresentationBatch> TakePending(Define.Scene destinationScene)
    {
        List<RewardPresentationBatch> result = new();
        for (int index = _pendingBatches.Count - 1; index >= 0; index--)
        {
            RewardPresentationBatch batch = _pendingBatches[index];
            if (batch.DestinationScene != destinationScene) continue;

            result.Add(batch);
            _pendingBatches.RemoveAt(index);
        }

        result.Reverse();
        return result;
    }
}
