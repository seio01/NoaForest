using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UI_BlessingSelectPopup : PopupBase
{
    [Header("Blessing List")]
    [SerializeField] private UI_BlessingEffectCollection blessingPrefab;
    [SerializeField] private RectTransform rectBlessingList;
    [SerializeField] private GameObject frameBlessingList;
    [SerializeField] private GameObject frameEmpty;

    [Header("Button")]
    [SerializeField] private ButtonBase buttonStart;

    private const int MAX_SELECTION_COUNT = 3;

    private List<UI_BlessingEffectCollection> _blessingItems = new();
    private List<BlessingSO> _selectedBlessings = new();
    private Action<List<BlessingSO>> _startAction;
    private bool _hasOwnedBlessings;
    private bool _isStarting;

    private BlessingGroupSO BlessingCatalog => Managers.GameData.Blessings;

    private async void Start()
    {
        if (buttonStart)
        {
            buttonStart.OnClick.AddListener(OnClickStart);
            buttonStart.Interactable = false;
        }

        if (blessingPrefab)
            blessingPrefab.gameObject.SetActive(false);

        await RenderBlessingsAsync();
        if (this && buttonStart)
            buttonStart.Interactable = true;
    }

    public void SetData(Action<List<BlessingSO>> startAction)
    {
        _startAction = startAction;
    }

    private async Task RenderBlessingsAsync()
    {
        ClearBlessingItems();
        List<BlessingSO> ownedBlessings = GetOwnedBlessings();
        _hasOwnedBlessings = ownedBlessings.Count > 0;
        SetEmptyState(!_hasOwnedBlessings);

        if (ownedBlessings.Count == 0 || !blessingPrefab || !rectBlessingList)
            return;

        await Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Blessing);
        if (!this)
            return;

        foreach (var blessingData in ownedBlessings)
        {
            var item = Instantiate(blessingPrefab, rectBlessingList, false);
            item.SetData(blessingData);
            item.Clicked += OnClickBlessing;
            item.gameObject.SetActive(true);
            _blessingItems.Add(item);
        }
    }

    private List<BlessingSO> GetOwnedBlessings()
    {
        BlessingGroupSO blessingCatalog = BlessingCatalog;
        Dictionary<string, int> blessingLevels = Managers.Data.CurrentSaveData.BlessingLevels;
        if (!blessingCatalog || blessingLevels == null || blessingLevels.Count == 0)
            return new List<BlessingSO>();

        return blessingCatalog.Blessings
            .Where(blessingData => blessingData && blessingLevels.ContainsKey(blessingData.Id))
            .OrderBy(blessingData => blessingData.Rarity)
            .ThenBy(blessingData => blessingData.DisplayName)
            .ToList();
    }

    private void ClearBlessingItems()
    {
        foreach (var item in _blessingItems)
        {
            Destroy(item.gameObject);
        }

        _blessingItems.Clear();
        _selectedBlessings.Clear();
    }

    private void SetEmptyState(bool isEmpty)
    {
        if (frameBlessingList)
            frameBlessingList.SetActive(!isEmpty);
        if (frameEmpty)
            frameEmpty.SetActive(isEmpty);
    }

    private void OnClickBlessing(UI_BlessingEffectCollection selectedItem)
    {
        BlessingSO blessingData = selectedItem.Data;
        if (!blessingData)
            return;

        //이미 선택된건 선택 해제
        if (_selectedBlessings.Remove(blessingData))
        {
            selectedItem.SetSelected(false);
            return;
        }

        if (_selectedBlessings.Count >= MAX_SELECTION_COUNT)
        {
            Managers.UI.ShowToast("가호는 최대 3개까지 선택할 수 있어요.");
            return;
        }

        _selectedBlessings.Add(blessingData);
        selectedItem.SetSelected(true);
    }

    private void OnClickStart()
    {
        if (_isStarting)
            return;

        if (!_hasOwnedBlessings || _selectedBlessings.Count > 0)
        {
            StartPurify();
            return;
        }

        OpenStartConfirmPopup();
    }

    private void OpenStartConfirmPopup()
    {
        Managers.UI.OpenPopup<UI_ConfirmPopup>("UI_ConfirmPopup", popup =>
        {
            popup.SetData(new ConfirmPopupData
            {
                title = "경고",
                info = "가호를 선택하지 않았습니다.\n이대로 정화를 시작하시겠어요?",
                hasImage = false,
                leftButtonData = new ConfirmPopupButtonData
                {
                    name = "취소",
                    color = ButtonColorType.White,
                    clickAction = Managers.UI.ClosePopup
                },
                rightButtonData = new ConfirmPopupButtonData
                {
                    name = "정화 시작",
                    color = ButtonColorType.Olive,
                    clickAction = StartPurify
                }
            });
        });
    }

    private void StartPurify()
    {
        if (_isStarting)
            return;

        _isStarting = true;
        if (buttonStart)
            buttonStart.Interactable = false;

        _startAction?.Invoke(new List<BlessingSO>(_selectedBlessings));
    }
}
