using System;
using System.Collections.Generic;

[Serializable]
public class CollectionUpgradeResponse
{
    public string CollectionType { get; set; }
    public string ItemId { get; set; }
    public int Level { get; set; }
    public string CurrencyType { get; set; }
    public int CurrencyBalance { get; set; }
    public int? PieceBalance { get; set; }
}

[Serializable]
public class NoaUnlockResponse
{
    public string ItemId { get; set; }
    public string CurrencyType { get; set; }
    public int CurrencyBalance { get; set; }
    public List<string> UnlockedNoaIds { get; set; }
}

[Serializable]
public class BlessingSummonResponse
{
    public string ItemId { get; set; }
    public string Rarity { get; set; }
    public bool IsNew { get; set; }
    public int Level { get; set; }
    public int AcquiredPieceCount { get; set; }
    public int PieceBalance { get; set; }
    public string CurrencyType { get; set; }
    public int CurrencyBalance { get; set; }
}
