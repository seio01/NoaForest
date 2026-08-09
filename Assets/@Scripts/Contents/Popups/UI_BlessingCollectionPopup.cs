using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UI_BlessingCollectionPopup : PopupBase
{
    [SerializeField] private UI_Collection collectionPrefab;
    [SerializeField] private RectTransform collectionParent;
    [SerializeField] private Sprite spriteUnacquired;

    private readonly List<UI_Collection> _collectionItems = new();

    private BlessingGroupSO BlessingCatalog => Managers.GameData.Blessings;

    private async void Start()
    {
        await PreloadBlessingAtlasesAsync();

        RenderBlessings();
    }

    private Task PreloadBlessingAtlasesAsync()
    {
        return Task.WhenAll(Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Blessing), Managers.ContentIcon.PreloadAsync(Define.ContentIconType.CommonUI));
    }

    private void RenderBlessings()
    {
        ClearBlessingList();
        BlessingGroupSO blessingCatalog = BlessingCatalog;
        if (!blessingCatalog || !collectionPrefab || !collectionParent)
            return;

        var sortedBlessings = blessingCatalog.Blessings.OrderBy(blessingData => blessingData.Rarity);
        foreach (var blessingData in sortedBlessings)
        {
            var item = Instantiate(collectionPrefab, collectionParent);
            item.SetData(CreateCollectionItemViewData(blessingData), () => OnClickBlessing(blessingData, item));
            _collectionItems.Add(item);
        }
    }

    private CollectionItemViewData CreateCollectionItemViewData(BlessingSO blessingData)
    {
        bool isLocked = !Managers.Collection.IsUnlocked(blessingData);
        int currentLevel = Managers.Collection.GetLevel(blessingData);
        int pieceCount = Managers.Collection.GetBlessingPieceCount(blessingData);
        int pieceCost = Managers.Collection.GetUpgradePieceCost(blessingData, currentLevel);
        bool isMaximumLevel = currentLevel >= BlessingSO.MAX_LEVEL || pieceCost <= 0;
        Sprite icon = isLocked ? spriteUnacquired : Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, blessingData.Id);
        Sprite rarityBadge = Managers.ContentIcon.GetBlessingRaritySprite(blessingData.Rarity);
        CollectionProgressViewData progress = new(
            isMaximumLevel ? 1 : pieceCount,
            isMaximumLevel ? 1 : pieceCost,
            isMaximumLevel);
        return new CollectionItemViewData(blessingData.DisplayName, icon, isLocked, rarityBadge, progress: progress);
    }

    private void ClearBlessingList()
    {
        foreach (var item in _collectionItems)
        {
            Destroy(item.gameObject);
        }

        _collectionItems.Clear();
    }

    private void OnClickBlessing(BlessingSO blessingData, UI_Collection collectionItem)
    {
        if (!blessingData)
            return;

        Managers.UI.OpenPopup<UI_BlessingCollectionInfoPopup>("UI_BlessingCollectionInfoPopup", popup =>
        {
            popup.SetData(blessingData, upgradedBlessing => RefreshCollectionItem(collectionItem, upgradedBlessing));
        });
    }

    private void RefreshCollectionItem(UI_Collection item, BlessingSO blessingData)
    {
        if (!item || !blessingData)
            return;

        item.SetData(CreateCollectionItemViewData(blessingData), () => OnClickBlessing(blessingData, item));
    }
}
