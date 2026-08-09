using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class NoaVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRendererNoa;
    [SerializeField] private Animator animatorNoa;
    [SerializeField] private Transform transformFirePoint;
    private static readonly int _attackHash = Animator.StringToHash("Attack");

    private NoaVisualSO _visualData;
    private PurifyPoolController _poolController;
    private float _localScaleX;
    private bool _isAttacking;

    public event Action AttackReleased;
    public event Action AttackFinished;

    private void Awake()
    {
        _localScaleX = Mathf.Abs(transform.localScale.x);
    }

    public bool SetData(NoaVisualSO visualData, Sprite fallbackSprite, PurifyPoolController poolController)
    {
        _visualData = visualData;
        _poolController = poolController;
        _isAttacking = false;

        animatorNoa.enabled = false;
        animatorNoa.runtimeAnimatorController = visualData ? visualData.AnimatorControllerNoa : null;
        spriteRendererNoa.sprite = fallbackSprite;
        if (!visualData || !animatorNoa.runtimeAnimatorController)
            return false;

        animatorNoa.enabled = true;
        animatorNoa.speed = 1f;
        animatorNoa.Rebind();
        animatorNoa.Update(0f);
        return true;
    }

    public bool PlayAttack(float attackInterval)
    {
        if (_isAttacking || !_visualData || !animatorNoa.runtimeAnimatorController)
            return false;

        _isAttacking = true;
        animatorNoa.speed = Mathf.Max(1f, _visualData.AttackAnimationDuration / Mathf.Max(0.01f, attackInterval));
        animatorNoa.ResetTrigger(_attackHash);
        animatorNoa.SetTrigger(_attackHash);
        return true;
    }

    public void FacePosition(Vector3 targetPosition)
    {
        float directionX = targetPosition.x - transform.position.x;
        if (Mathf.Abs(directionX) <= 0.001f)
            return;

        Vector3 localScale = transform.localScale;
        localScale.x = directionX > 0f ? -_localScaleX : _localScaleX;
        transform.localScale = localScale;
    }

    public void PlayAttackEffect(Vector3 targetPosition)
    {
        if (!_visualData || !_visualData.AttackEffectPrefab || !_poolController || !transformFirePoint)
            return;

        GameObject effectObject = _poolController.Get(_visualData.AttackEffectPrefab.gameObject, _visualData.EffectPoolCapacity);
        if (!effectObject || !effectObject.TryGetComponent(out NoaAttackEffect attackEffect))
            return;

        bool areEffectsEnabled = Managers.UserSetting.CurrentSetting.IsEffectEnabled;
        attackEffect.Play(_visualData, _poolController, transformFirePoint.position, targetPosition, areEffectsEnabled);
        Managers.Sound.Play(_visualData.AttackSfx, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
    }

    public void OnAttackRelease()
    {
        if (_isAttacking)
            AttackReleased?.Invoke();
    }

    public void OnAttackFinished()
    {
        if (!_isAttacking)
            return;

        _isAttacking = false;
        animatorNoa.speed = 1f;
        AttackFinished?.Invoke();
    }

    public void ResetVisual()
    {
        _isAttacking = false;
        if (animatorNoa)
        {
            animatorNoa.ResetTrigger(_attackHash);
            animatorNoa.speed = 1f;
        }
    }

    private void OnDisable()
    {
        ResetVisual();
    }
}
