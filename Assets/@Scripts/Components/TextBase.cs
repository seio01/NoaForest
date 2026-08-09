using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextBase : SerializedMonoBehaviour
{
    [OdinSerialize, SerializeField] private Dictionary<Define.TextColorPalette, Material> mainOutlineMaterialMap;
    [OdinSerialize, SerializeField] private Dictionary<Define.TextColorPalette, Material> subOutlineMaterialMap;
    [OdinSerialize, SerializeField] private Dictionary<Define.TextFont, TMP_FontAsset> fontAssetMap;
    [OdinSerialize, SerializeField] private Dictionary<Define.TextColorPalette, Color> colorMap;
    [Tooltip("원하는 스타일을 적용해주세요.")] 
    [SerializeField] private Define.TextFont textFont = Define.TextFont.Main;
    [SerializeField] private Define.TextColorPalette textColor = Define.TextColorPalette.Brown1;
    [SerializeField] private Define.TextColorPalette textOutline = Define.TextColorPalette.None;

    private TextMeshProUGUI _textTarget;

    private void Awake()
    {
        _textTarget = GetComponent<TextMeshProUGUI>();
        SetTextStyle();
    }

    private void OnValidate()
    {
        if (_textTarget == null)
            _textTarget = GetComponent<TextMeshProUGUI>();

        SetTextStyle();
    }

    public string text
    {
        get => _textTarget ? _textTarget.text : string.Empty;
        set
        {
            if (!_textTarget)
                _textTarget = GetComponent<TextMeshProUGUI>();

            _textTarget.text = value;
        }
    }

    public void SetTextStyle()
    {
        if(_textTarget == null)
            return;

        SetTextFont(textFont);
        SetTextColor(textColor);
        SetTextOutline(textOutline);

        _textTarget.SetAllDirty();
    }

    public void SetTextFont(Define.TextFont font)
    {
        if(font == Define.TextFont.None)
            return;
        if(fontAssetMap == null)
            return;
        if(fontAssetMap.TryGetValue(font, out TMP_FontAsset fontAsset))
        {
            _textTarget.font = fontAsset;
        }
        else
        {
            Debug.LogWarning($"[TextStyleApplier] Font asset for {font} not found.");
        }
    }

    public void SetTextColor(Define.TextColorPalette color)
    {
        if(color == Define.TextColorPalette.None)
            return;
        if(colorMap == null)
            return;

        if(colorMap.TryGetValue(color, out Color textColor))
        {
            this.textColor = color;
            _textTarget.color = textColor;
        }
        else
        {
            Debug.LogWarning($"[TextStyleApplier] Color for {color} not found.");
        }
    }

    public void SetTextOutline(Define.TextColorPalette outline)
    {
        if(textFont == Define.TextFont.None)
            return;

        if(textFont == Define.TextFont.Main)
        {
            SetTextMaterial(mainOutlineMaterialMap, outline);
        }
        else if(textFont == Define.TextFont.Sub)
        {
            SetTextMaterial(subOutlineMaterialMap, outline);
        }
    }

    private void SetTextMaterial(Dictionary<Define.TextColorPalette, Material> materialMap, Define.TextColorPalette key)
    {
        if(materialMap == null)
            return;
        
        if(materialMap.TryGetValue(key, out Material mat))
        {
            _textTarget.fontMaterial = mat;
        }
        else
        {
            Debug.LogWarning($"[TextStyleApplier] Material for {key} not found.");
        }
    }

}
