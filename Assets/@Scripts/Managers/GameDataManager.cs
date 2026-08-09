using System;
using System.Threading.Tasks;
using UnityEngine;

public class GameDataManager
{
    private const string GAME_DATA_CATALOG_PATH = "Data/GameDataCatalogSO";

    private Task _initializeTask;

    public GameDataCatalogSO Catalog { get; private set; }
    public bool IsInitialized => Catalog;
    public NoaGroupSO Noas => Catalog ? Catalog.Noas : null;
    public StageGroupSO Stages => Catalog ? Catalog.Stages : null;
    public MoteGroupSO Motes => Catalog ? Catalog.Motes : null;
    public BlessingGroupSO Blessings => Catalog ? Catalog.Blessings : null;
    public PurifyBalanceSO PurifyBalance => Catalog ? Catalog.PurifyBalance : null;

    public Task InitializeAsync()
    {
        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    private async Task InitializeInternalAsync()
    {
        Catalog = await Managers.Resource.LoadTaskAsync<GameDataCatalogSO>(GAME_DATA_CATALOG_PATH);
        if (!Catalog)
            throw new InvalidOperationException($"Game data catalog is missing: Resources/{GAME_DATA_CATALOG_PATH}");

        if (!Catalog.Noas || !Catalog.Stages || !Catalog.Motes || !Catalog.Blessings || !Catalog.PurifyBalance)
            throw new InvalidOperationException("[GameDataManager] A required game data reference is missing.");

        Debug.Log("[GameDataManager] Initialized.");
    }
}
