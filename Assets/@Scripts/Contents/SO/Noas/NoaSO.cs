using UnityEngine;

[CreateAssetMenu(fileName = "NoaSO", menuName = "Noa Forest/Purify/Noa")]
public class NoaSO : ScriptableObject, ICollectionItem
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Define.ElementType element;
    [SerializeField] private Define.NoaTier tier;
    [SerializeField] private int unlockCost;
    [SerializeField] private NoaSO nextTierNoa;

    public string Id => id;
    public Define.CollectionType CollectionType => Define.CollectionType.Noa;
    public string DisplayName => displayName;
    public string Description => description;
    public Define.ElementType Element => element;
    public Define.NoaTier Tier => tier;
    public int UnlockCost => unlockCost;
    public NoaSO NextTierNoa => nextTierNoa;
    public string IconId => $"noa_{element.ToString().ToLowerInvariant()}_{(int)tier:00}";

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.Log($"[NoaSO] Id is empty: {name}");
        }

        if (element == Define.ElementType.Neutral)
        {
            Debug.Log($"[NoaSO] Neutral element is not valid for Noa: {id}");
        }

        if (tier == Define.NoaTier.Tier1 && unlockCost != 0)
            Debug.Log($"[NoaSO] Tier1 Noa unlock cost must be zero: {id}");
        if (tier != Define.NoaTier.Tier1 && unlockCost <= 0)
            Debug.Log($"[NoaSO] Unlock cost must be positive: {id}");

        if (tier == Define.NoaTier.Tier3)
        {
            if (nextTierNoa)
            {
                Debug.Log($"[NoaSO] Tier3 Noa cannot have a next tier: {id}");
            }

            return;
        }

        if (!nextTierNoa)
        {
            Debug.LogError($"[NoaSO] Next tier Noa is missing: {id}");
            return;
        }

        Define.NoaTier expectedTier = (Define.NoaTier)((int)tier + 1);
        if (nextTierNoa.Element != element || nextTierNoa.Tier != expectedTier)
        {
            Debug.Log(
                $"[NoaSO] Invalid next tier reference: {id} -> {nextTierNoa.Id}");
        }
    }
}
