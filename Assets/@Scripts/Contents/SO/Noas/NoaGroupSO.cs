using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NoaGroupSO", menuName = "Noa Forest/Purify/Noa Group")]
public class NoaGroupSO : ScriptableObject
{
    [SerializeField] private NoaSO[] noas = Array.Empty<NoaSO>();
    [SerializeField] private NoaStatsSO stats;

    public NoaSO[] Noas => noas;
    public NoaStatsSO Stats => stats;

    public NoaSO GetNoa(string id)
    {
        foreach (NoaSO data in noas)
        {
            if (!data ||
                !string.Equals(data.Id, id, StringComparison.Ordinal))
            {
                continue;
            }

            return data;
        }

        return null;
    }

    public NoaSO GetNoa(Define.ElementType element, Define.NoaTier tier)
    {
        foreach (NoaSO data in noas)
        {
            if (!data ||
                data.Element != element ||
                data.Tier != tier)
            {
                continue;
            }

            return data;
        }

        return null;
    }

    private void OnValidate()
    {
        if (!stats)
        {
            Debug.LogError("[NoaGroupSO] NoaStatsSO is missing.");
        }

        HashSet<string> registeredIds = new(StringComparer.Ordinal);
        HashSet<(Define.ElementType, Define.NoaTier)> registeredData = new();

        foreach (NoaSO noa in noas)
        {
            if (!noa)
            {
                Debug.LogError("[NoaGroupSO] Null NoaSO is registered.");
                continue;
            }

            if (!registeredIds.Add(noa.Id))
            {
                Debug.Log($"[NoaGroupSO] Duplicate Id: {noa.Id}");
            }

            if (!registeredData.Add((noa.Element, noa.Tier)))
            {
                Debug.Log(
                    $"[NoaGroupSO] Duplicate element and tier: {noa.Element}, {noa.Tier}");
            }
        }

        foreach (Define.ElementType element in ElementUtility.Elements)
        {
            foreach (Define.NoaTier tier in Enum.GetValues(typeof(Define.NoaTier)))
            {
                if (!registeredData.Contains((element, tier)))
                {
                    Debug.LogError(
                        $"[NoaGroupSO] Missing NoaSO: {element}, {tier}");
                }
            }
        }
    }
}
