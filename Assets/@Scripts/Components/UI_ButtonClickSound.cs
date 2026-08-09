using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UI_ButtonClickSound : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (GetComponent<ButtonBase>()) return;

        if (!_button) _button = GetComponent<Button>();
        if (_button) _button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        if (_button) _button.onClick.RemoveListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        Managers.Sound.Play(Define.AudioClip.ButtonClick, Define.AudioSourceType.Sfx, Define.AudioPath.Common);
    }
}
