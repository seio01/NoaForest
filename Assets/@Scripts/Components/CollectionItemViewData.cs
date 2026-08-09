using UnityEngine;

public class CollectionProgressViewData
{
    public int current;
    public int required;
    public bool isMaximum;
    public string displayText;

    public CollectionProgressViewData(int current, int required, bool isMaximum, string displayText = null)
    {
        this.current = current;
        this.required = required;
        this.isMaximum = isMaximum;
        this.displayText = displayText;
    }
}

public class CollectionItemViewData
{
    public string name;    
    public Sprite icon;
    public bool isLocked;
    public Sprite rarityBadge;
    public int? starCount;
    public CollectionProgressViewData progress;

    public CollectionItemViewData(
        string name,
        Sprite icon,
        bool isLocked = false,
        Sprite rarityBadge = null,
        int? starCount = null,
        CollectionProgressViewData progress = null)
    {
        this.name = name;
        this.icon = icon;
        this.isLocked = isLocked;
        this.rarityBadge = rarityBadge;
        this.starCount = starCount;
        this.progress = progress;
    }
}
