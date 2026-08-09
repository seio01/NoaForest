using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PurifyNoaVisualEntry
{
    [SerializeField] private NoaSO noaData;
    [SerializeField] private NoaVisualSO noaVisual;

    public NoaSO NoaData => noaData;
    public NoaVisualSO NoaVisual => noaVisual;
}

[CreateAssetMenu(fileName = "PurifyNoaVisualCatalogSO", menuName = "Noa Forest/Purify/Noa Visual Catalog")]
public class PurifyNoaVisualCatalogSO : ScriptableObject
{
    [SerializeField] private List<PurifyNoaVisualEntry> noaVisualEntries = new();

    public NoaVisualSO GetVisual(NoaSO noaData)
    {
        if (!noaData) return null;

        foreach (PurifyNoaVisualEntry entry in noaVisualEntries)
        {
            if (entry != null && entry.NoaData == noaData)
                return entry.NoaVisual;
        }

        return null;
    }

    private void OnValidate()
    {
        HashSet<NoaSO> registeredNoas = new();
        foreach (PurifyNoaVisualEntry entry in noaVisualEntries)
        {
            if (entry == null || !entry.NoaData || !entry.NoaVisual)
            {
                Debug.LogError($"[PurifyNoaVisualCatalogSO] Invalid entry: {name}");
                continue;
            }

            if (!registeredNoas.Add(entry.NoaData))
                Debug.LogError($"[PurifyNoaVisualCatalogSO] Duplicate Noa: {entry.NoaData.Id}");
        }
    }
}
