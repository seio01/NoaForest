using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

public class ContentIconManager : IDisposable
{
    private static readonly Dictionary<Define.ContentIconType, string>
        ATLAS_PATHS = new Dictionary<Define.ContentIconType, string>
        {
            { Define.ContentIconType.Noa, "Atlases/NoaIconAtlas" },
            { Define.ContentIconType.Mote, "Atlases/MoteIconAtlas" },
            { Define.ContentIconType.Blessing, "Atlases/BlessingIconAtlas" },
            { Define.ContentIconType.CommonUI, "Atlases/CommonUIAtlas" }
        };

    private static readonly Dictionary<Define.CurrencyType, string> _currencyIconNames =
        new Dictionary<Define.CurrencyType, string>
        {
            { Define.CurrencyType.Seed, "icon_currency_seed" },
            { Define.CurrencyType.ElementCore, "icon_currency_element" },
            { Define.CurrencyType.Energy, "icon_currency_energy" },
            { Define.CurrencyType.NoaMemory, "icon_currency_noa_memory" },
            { Define.CurrencyType.BlessingTicket, "icon_currency_blessing_ticket" }
        };

    private readonly Dictionary<Define.ContentIconType, SpriteAtlas> _atlases = new();
    private readonly Dictionary<Define.ContentIconType, Task<SpriteAtlas>> _atlasLoadTasks = new();
    private readonly Dictionary<(Define.ContentIconType, string), Sprite> _sprites = new();

    public async Task PreloadAsync(Define.ContentIconType iconType)
    {
        await LoadAtlasAsync(iconType);
    }

    public async Task<Sprite> LoadSpriteAsync(Define.ContentIconType iconType, string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return null;
        }

        (Define.ContentIconType, string) key = (iconType, contentId);
        if (_sprites.TryGetValue(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        await LoadAtlasAsync(iconType);
        return GetLoadedSprite(iconType, contentId);
    }

    public Sprite GetLoadedSprite(Define.ContentIconType iconType, string contentId)
    {
        (Define.ContentIconType, string) key = (iconType, contentId);
        if (_sprites.TryGetValue(key, out Sprite sprite))
            return sprite;
        if (!_atlases.TryGetValue(iconType, out SpriteAtlas atlas))
            return null;

        sprite = atlas.GetSprite(contentId);
        if (sprite)
            _sprites[key] = sprite;

        return sprite;
    }

    public Sprite GetBlessingRaritySprite(Define.BlessingRarity rarity)
    {
        string spriteName = rarity switch
        {
            Define.BlessingRarity.Common => "icon_badge_common",
            Define.BlessingRarity.Rare => "icon_badge_rare",
            Define.BlessingRarity.Epic => "icon_badge_epic",
            Define.BlessingRarity.Legendary => "icon_badge_legendary",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(spriteName))
            return null;

        return GetLoadedSprite(Define.ContentIconType.CommonUI, spriteName);
    }

    public Sprite GetCurrencyIcon(Define.CurrencyType currencyType)
    {
        return _currencyIconNames.TryGetValue(currencyType, out string spriteName)
            ? GetLoadedSprite(Define.ContentIconType.CommonUI, spriteName)
            : null;
    }

    public Sprite GetElementSprite(Define.ElementType elementType)
    {
        string spriteName = elementType switch
        {
            Define.ElementType.Water => "icon_element_water",
            Define.ElementType.Fire => "icon_element_fire",
            Define.ElementType.Wind => "icon_element_wind",
            Define.ElementType.Earth => "icon_element_earth",
            _ => string.Empty
        };
        return string.IsNullOrEmpty(spriteName)
            ? null
            : GetLoadedSprite(Define.ContentIconType.CommonUI, spriteName);
    }

    public void Unload(Define.ContentIconType iconType)
    {
        List<(Define.ContentIconType, string)> spriteKeys = new();
        foreach (KeyValuePair<(Define.ContentIconType, string), Sprite> entry in _sprites)
        {
            if (entry.Key.Item1 != iconType)
            {
                continue;
            }

            if (entry.Value != null && Application.isPlaying)
            {
                Object.Destroy(entry.Value);
            }

            spriteKeys.Add(entry.Key);
        }

        foreach ((Define.ContentIconType, string) spriteKey in spriteKeys)
        {
            _sprites.Remove(spriteKey);
        }

        if (!_atlases.Remove(iconType, out SpriteAtlas atlas))
        {
            return;
        }

        string path = GetAtlasPath(iconType);
        Managers.Resource.Unload(path);

        if (atlas != null && Application.isPlaying)
        {
            Resources.UnloadAsset(atlas);
        }
    }

    public void Dispose()
    {
        Define.ContentIconType[] iconTypes = (Define.ContentIconType[])Enum.GetValues(typeof(Define.ContentIconType));

        foreach (Define.ContentIconType iconType in iconTypes)
        {
            Unload(iconType);
        }

        _atlasLoadTasks.Clear();
    }

    private async Task<SpriteAtlas> LoadAtlasAsync(Define.ContentIconType iconType)
    {
        if (_atlases.TryGetValue(iconType, out SpriteAtlas cachedAtlas))
        {
            return cachedAtlas;
        }

        if (!_atlasLoadTasks.TryGetValue(iconType, out Task<SpriteAtlas> loadTask))
        {
            string path = GetAtlasPath(iconType);
            loadTask = Managers.Resource.LoadTaskAsync<SpriteAtlas>(path);
            _atlasLoadTasks.Add(iconType, loadTask);
        }

        try
        {
            SpriteAtlas atlas = await loadTask;
            _atlases[iconType] = atlas;
            return atlas;
        }
        finally
        {
            _atlasLoadTasks.Remove(iconType);
        }
    }

    private string GetAtlasPath(Define.ContentIconType iconType)
    {
        if (ATLAS_PATHS.TryGetValue(iconType, out string path))
        {
            return path;
        }

        return "";
    }

}
