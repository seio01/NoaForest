using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoteGroupSO", menuName = "Noa Forest/Purify/Mote Group")]
public class MoteGroupSO : ScriptableObject
{
    [SerializeField] private MoteSO[] motes = Array.Empty<MoteSO>();

    public MoteSO[] Motes => motes;

    public MoteSO GetMote(string id)
    {
        foreach (MoteSO data in motes)
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

    public MoteSO GetMote(Define.ElementType element)
    {
        foreach (MoteSO data in motes)
        {
            if (!data || data.Element != element)
            {
                continue;
            }

            return data;
        }

        return null;
    }

    private void OnValidate()
    {
        HashSet<string> registeredIds = new(StringComparer.Ordinal);
        HashSet<Define.ElementType> registeredElements = new();

        foreach (MoteSO mote in motes)
        {
            if (!mote)
            {
                Debug.LogError("[MoteGroupSO] Null MoteSO is registered.");
                continue;
            }

            if (!registeredIds.Add(mote.Id))
            {
                Debug.Log($"[MoteGroupSO] Duplicate Id: {mote.Id}");
            }

            if (!registeredElements.Add(mote.Element))
            {
                Debug.Log($"[MoteGroupSO] Duplicate element: {mote.Element}");
            }
        }

        foreach (Define.ElementType element in Enum.GetValues(typeof(Define.ElementType)))
        {
            if (!registeredElements.Contains(element))
            {
                Debug.LogError($"[MoteGroupSO] Missing MoteSO: {element}");
            }
        }
    }
}
