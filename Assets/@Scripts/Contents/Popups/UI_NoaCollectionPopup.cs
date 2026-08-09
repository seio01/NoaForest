using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UI_NoaCollectionPopup : PopupBase
{
    [SerializeField] private UI_Collection collectionPrefab;
    [SerializeField] private RectTransform collectionParent;

    private readonly List<UI_Collection> _collectionItems = new();

    private NoaGroupSO NoaCatalog => Managers.GameData.Noas;

    private async void Start()
    {
        await PreloadCollectionAtlasesAsync();

        RenderNoas();
    }

    private Task PreloadCollectionAtlasesAsync()
    {
        return Task.WhenAll(
            Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Noa),
            Managers.ContentIcon.PreloadAsync(Define.ContentIconType.CommonUI));
    }

    private void RenderNoas()
    {
        ClearNoaList();
        NoaGroupSO noaCatalog = NoaCatalog;
        if (!noaCatalog || !collectionPrefab || !collectionParent)
            return;

        var sortedNoas = noaCatalog.Noas
            .Where(noaData => noaData)
            .OrderBy(noaData => ElementUtility.GetIndex(noaData.Element))
            .ThenBy(noaData => noaData.Tier);
        foreach (NoaSO noaData in sortedNoas)
        {
            UI_Collection item = Instantiate(collectionPrefab, collectionParent);
            item.SetData(CreateCollectionItemViewData(noaData), () => OnClickNoa(noaData, item));
            _collectionItems.Add(item);
        }
    }

    private CollectionItemViewData CreateCollectionItemViewData(NoaSO noaData)
    {
        int currentLevel = Managers.Collection.GetLevel(noaData);
        int maximumLevel = Managers.Collection.GetMaximumLevel(noaData);
        bool isMaximumLevel = currentLevel >= maximumLevel;
        bool isLocked = !Managers.Collection.IsUnlocked(noaData);
        Sprite icon = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, noaData.IconId);
        CollectionProgressViewData progress = new(currentLevel, maximumLevel, isMaximumLevel, $"LV.{currentLevel}");
        return new CollectionItemViewData(noaData.DisplayName, icon, isLocked, starCount: (int)noaData.Tier, progress: progress);
    }

    private void ClearNoaList()
    {
        foreach (UI_Collection item in _collectionItems)
        {
            if (item)
                Destroy(item.gameObject);
        }

        _collectionItems.Clear();
    }

    private void OnClickNoa(NoaSO noaData, UI_Collection collectionItem)
    {
        if (!noaData)
            return;

        Managers.UI.OpenPopup<UI_NoaCollectionInfoPopup>("UI_NoaCollectionInfoPopup", popup =>
        {
            popup.SetData(noaData, changedNoa => RefreshCollectionItem(collectionItem, changedNoa));
        });
    }

    private void RefreshCollectionItem(UI_Collection item, NoaSO noaData)
    {
        if (!item || !noaData)
            return;

        item.SetData(CreateCollectionItemViewData(noaData), () => OnClickNoa(noaData, item));
    }
}
