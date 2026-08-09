using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupBase : UI_Base
{
    [Header("Popup Options")]
    [SerializeField] protected bool enableSound = true;
    [SerializeField] protected bool enableAnimation = true;
    [SerializeField] protected bool enableBackgroundClick = true;
    [SerializeField] protected bool allowDuplicatePopup = false;
    [SerializeField] protected bool showCurrencyAbovePopup = false;
    [HideInInspector] public string PopupKey;
    private GraphicRaycaster _raycaster;
    private Sequence _seq;

    public bool AllowDuplicatePopup => allowDuplicatePopup;
    public bool EnableBackgroundClick => enableBackgroundClick;
    public bool ShowCurrencyAbovePopup => showCurrencyAbovePopup;

    public event Action OnBeforeClose = null; //닫기 전 시점
    public event Action OnAfterClose = null; //닫기 완료 시점

    //팝업 초기화 필수 항목 정의
    private void Awake()
    {
        _raycaster = Utils.GetorAddComponent<GraphicRaycaster>(gameObject);
        BindCloseButton();
        OnAwake();
    }

    protected virtual void OnAwake()
    {
        
    }

    private void OnDestroy()
    {
        _seq?.Kill();
        Destroy();
    }

    protected virtual void Destroy()
    {
        
    }

    public void OnOpen()
    {
        PlayAnimation(true);
        PlaySound(true);
    }

    public void OnClose()
    {
        OnBeforeClose?.Invoke();

        PlaySound(false);

        if(enableAnimation)
        {
            PlayAnimation(false);
        }
        else
        {
            OnAfterClose?.Invoke();
            Destroy(gameObject);
        }
    }

    private void PlayAnimation(bool isOpen)
    {
        if(!enableAnimation) return;

        var obj = Utils.FindChild<Transform>(gameObject, "ImageBackground", false);
        if(obj == null) 
        {   
            if(!isOpen)
            {
                OnAfterClose?.Invoke();
                Destroy(gameObject);
            }
            return;
        }

        SetInputEnabled(false);

        _seq?.Kill();
        _seq = DOTween.Sequence();

        if(isOpen)
        {
            obj.transform.localScale = Vector3.zero;
            _seq.Append(obj.DOScale(new Vector3(1.15f, 1.15f, 1.15f), 0.2f))
                    .Append(obj.DOScale(Vector3.one, 0.05f))
                    .OnComplete(() => SetInputEnabled(true));
            _seq.Play();
        }
        else
        {
            obj.transform.localScale = Vector3.one;
            _seq.Append(obj.DOScale(new Vector3(1.075f, 1.075f, 1.075f), 0.05f))
                    .Append(obj.DOScale(Vector3.zero, 0.125f))
                    .OnComplete(() =>
                    {
                        OnAfterClose?.Invoke();
                        Destroy(gameObject);
                    });
            _seq.Play();
        }

    }

    private void PlaySound(bool isOpen)
    {
        if(!enableSound) return; 
    }

    private void SetInputEnabled(bool enable)
    {
        if(_raycaster == null) return;

        _raycaster.enabled = enable;
    }

    private void BindCloseButton()
    {
        var button = Utils.FindChild<ButtonBase>(gameObject, "ButtonClose", false);
        if(button)
        {
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener(() =>
            {
                //팝업 닫기
                Managers.UI.ClosePopup();
            });
        }
    }

}
