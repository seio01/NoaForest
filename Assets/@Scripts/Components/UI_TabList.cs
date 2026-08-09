using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_TabList : UI_Base
{
    [SerializeField] private UI_Tab tabPrefab;
    [SerializeField] private RectTransform rectTabRoot;

    private readonly List<UI_Tab> _tabs = new();
    private Action<int> _onTabSelected;

    public int SelectedIndex { get; private set; } = -1;

    public void SetData(List<UI_TabData> tabDataList, Action<int> onTabSelected)
    {
        Clear();
        _onTabSelected = onTabSelected;

        if (tabDataList == null || tabPrefab == null || rectTabRoot == null)
            return;

        for (int index = 0; index < tabDataList.Count; index++)
        {
            UI_Tab tab = Instantiate(tabPrefab, rectTabRoot, false);
            tab.SetData(tabDataList[index]);
            tab.Clicked += OnTabClicked;
            _tabs.Add(tab);
        }

        if (_tabs.Count > 0)
            Select(0);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            return;

        SelectedIndex = index;

        for (int tabIndex = 0; tabIndex < _tabs.Count; tabIndex++)
            _tabs[tabIndex].SetSelected(tabIndex == index);

        _onTabSelected?.Invoke(index);
    }

    public void Clear()
    {
        foreach (UI_Tab tab in _tabs)
        {
            if (!tab) continue;

            tab.Clicked -= OnTabClicked;
            tab.gameObject.SetActive(false);
            Destroy(tab.gameObject);
        }

        _tabs.Clear();
        SelectedIndex = -1;
        _onTabSelected = null;
    }

    private void OnTabClicked(UI_Tab selectedTab)
    {
        int index = _tabs.IndexOf(selectedTab);
        if (index >= 0)
            Select(index);
    }
}
