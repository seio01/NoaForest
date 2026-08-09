using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mote : MonoBehaviour, IPoolable, IPoolOrderReceiver
{
    [SerializeField] private Transform transformVisualRoot;
    [SerializeField] private SpriteRenderer spriteRendererMote;
    [SerializeField] private Animator animatorMote;
    [SerializeField] private Slider sliderHealth;
    private static readonly int WALK_STATE_HASH = Animator.StringToHash("Base Layer.Walk");

    private PurifyRoutePoint[] _route;
    private MoteSO _data;
    private Action<Mote, float> _onDamaged;
    private Action<Mote> _onRouteCompleted;
    private Action<Mote> _onDefeated;
    private Coroutine _moveCoroutine;
    private int _nextPointIndex;
    private float _currentHealth;
    private float _maximumHealth;
    private int _escapeDamage;

    public int EscapeDamage => _escapeDamage;
    public int KillReward => _data ? _data.KillReward : 0;
    public Define.ElementType Element => _data ? _data.Element : Define.ElementType.Neutral;
    public bool IsTargetable => isActiveAndEnabled && _data && _currentHealth > 0f;
    public Vector3 DamageFontWorldPosition => sliderHealth ? sliderHealth.transform.position : transform.position;

    public void Init(
        MoteSO data,
        Sprite spriteMote,
        RuntimeAnimatorController animatorController,
        PurifyRoutePoint[] route,
        PurifyRouteSide routeSide,
        float healthMultiplier,
        int escapeDamage,
        Action<Mote, float> onDamaged,
        Action<Mote> onDefeated,
        Action<Mote> onRouteCompleted)
    {

        StopMovement();

        _data = data;
        _route = route;
        _onDamaged = onDamaged;
        _onDefeated = onDefeated;
        _onRouteCompleted = onRouteCompleted;
        _maximumHealth = data.Health * healthMultiplier;
        _currentHealth = _maximumHealth;
        _escapeDamage = escapeDamage;
        _nextPointIndex = 1;

        UpdateSlider();
        transform.position = route[0].Position;
        ApplyVisuals(spriteMote, animatorController, routeSide);
        _moveCoroutine = StartCoroutine(MoveRoutine());
    }

    public bool TakeDamage(float damage)
    {
        if (!IsTargetable || damage <= 0f)
        {
            return false;
        }

        float appliedDamage = Mathf.Min(_currentHealth, damage);
        _currentHealth -= appliedDamage;
        UpdateSlider();
        _onDamaged?.Invoke(this, appliedDamage);

        if (_currentHealth > 0f)
        {
            return false;
        }

        StopMovement();
        _onRouteCompleted = null;

        Action<Mote> defeatedCallback = _onDefeated;
        _onDefeated = null;
        defeatedCallback?.Invoke(this);
        return true;
    }

    public void OnGet()
    {
        ResetRuntimeState();
    }

    public void OnRelease()
    {
        ResetRuntimeState();
    }

    public void SetPoolOrder(int poolOrder)
    {
        Vector3 localPosition = transformVisualRoot.localPosition;
        localPosition.z = -poolOrder * 0.01f;
        transformVisualRoot.localPosition = localPosition;
    }

    private void ResetRuntimeState()
    {
        StopMovement();
        _data = null;
        _route = null;
        _onDamaged = null;
        _onDefeated = null;
        _onRouteCompleted = null;
        _currentHealth = 0f;
        _maximumHealth = 0f;
        _escapeDamage = 0;
        _nextPointIndex = 0;
        UpdateSlider();
        ResetVisuals();
    }

    private void UpdateSlider()
    {
        if (!sliderHealth)
        {
            return;
        }

        sliderHealth.minValue = 0f;
        sliderHealth.maxValue = Mathf.Max(1f, _maximumHealth);
        sliderHealth.value = _currentHealth;
    }

    private IEnumerator MoveRoutine()
    {
        while (_nextPointIndex < _route.Length)
        {
            PurifyRoutePoint destinationPoint = _route[_nextPointIndex];
            Vector3 destination = destinationPoint.Position;
            transform.position = Vector3.MoveTowards(transform.position, destination, _data.MoveSpeed * Time.deltaTime);

            if (Vector3.SqrMagnitude(transform.position - destination) <= 0.01f * 0.01f)
            {
                transform.position = destination;
                if (destinationPoint.ShouldToggleFacingOnDeparture)
                {
                    spriteRendererMote.flipX = !spriteRendererMote.flipX;
                }

                _nextPointIndex++;
            }

            yield return null;
        }

        _moveCoroutine = null;
        Action<Mote> completionCallback = _onRouteCompleted;
        _onRouteCompleted = null;
        completionCallback?.Invoke(this);
    }

    private void StopMovement()
    {
        if (_moveCoroutine == null) return;

        StopCoroutine(_moveCoroutine);
        _moveCoroutine = null;
    }

    private void ApplyVisuals(Sprite spriteMote, RuntimeAnimatorController animatorController, PurifyRouteSide routeSide)
    {
        spriteRendererMote.sprite = spriteMote;
        spriteRendererMote.flipX = routeSide == PurifyRouteSide.Right;
        spriteRendererMote.color = Color.white;

        animatorMote.enabled = false;
        animatorMote.runtimeAnimatorController = animatorController;
        animatorMote.enabled = true;
        animatorMote.Rebind();

        animatorMote.Play(WALK_STATE_HASH, 0, 0f);
        animatorMote.Update(0f);
    }

    private void ResetVisuals()
    {
        if (animatorMote)
        {
            animatorMote.enabled = false;
            animatorMote.runtimeAnimatorController = null;
            animatorMote.speed = 1f;
        }

        if (spriteRendererMote)
        {
            spriteRendererMote.sprite = null;
            spriteRendererMote.flipX = false;
            spriteRendererMote.color = Color.white;
        }
    }

}
