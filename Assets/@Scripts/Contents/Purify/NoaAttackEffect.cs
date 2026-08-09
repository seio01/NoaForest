using UnityEngine;

[RequireComponent(typeof(PooledObject), typeof(SpriteRenderer), typeof(Animator))]
public class NoaAttackEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer spriteRendererEffect;
    [SerializeField] private Animator animatorEffect;
    [SerializeField] private float floatHitDistance = 0.05f;

    private NoaVisualSO _visualData;
    private PurifyPoolController _poolController;
    private NoaParticleEffect _particleTrail;
    private Vector3 _attackDirection;
    private Vector3 _targetPosition;
    private bool _isMoving;

    public void Play(NoaVisualSO visualData, PurifyPoolController poolController, Vector3 origin, Vector3 targetPosition, bool areEffectsEnabled)
    {
        _visualData = visualData;
        _poolController = poolController;
        _attackDirection = targetPosition - origin;
        _targetPosition = targetPosition;
        transform.position = origin;
        transform.localScale = Vector3.one * visualData.AttackEffectScale;
        ApplyVisual();

        ApplyFacingDirection();

        if (areEffectsEnabled)
            PlayTrail();

        _isMoving = true;
    }

    public void OnGet()
    {
        ResetState();
    }

    public void OnRelease()
    {
        ResetState();
        _visualData = null;
        _poolController = null;
    }

    private void Update()
    {
        if (!_isMoving || !_visualData)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _visualData.AttackEffectSpeed * Time.deltaTime);
        if ((transform.position - _targetPosition).sqrMagnitude <= floatHitDistance * floatHitDistance)
            Complete();
    }

    private void ApplyVisual()
    {
        spriteRendererEffect.sprite = _visualData.AttackEffectSprite;
        spriteRendererEffect.enabled = spriteRendererEffect.sprite;

        animatorEffect.enabled = false;
        animatorEffect.runtimeAnimatorController = _visualData.AnimatorControllerAttackEffect;
        if (animatorEffect.runtimeAnimatorController)
        {
            animatorEffect.enabled = true;
            animatorEffect.Rebind();
            animatorEffect.Update(0f);
        }
    }

    private void PlayTrail()
    {
        NoaParticleEffect trailPrefab = _visualData.ParticleTrailPrefab;
        if (!trailPrefab)
            return;

        GameObject trailObject = _poolController.Get(trailPrefab.gameObject, _visualData.EffectPoolCapacity);
        if (trailObject && trailObject.TryGetComponent(out NoaParticleEffect trail))
        {
            _particleTrail = trail;
            _particleTrail.PlayTrail(_poolController, transform, _visualData.ParticleColorMin, _visualData.ParticleColorMax, _visualData.TrailEmissionRate);
        }
    }

    private void Complete()
    {
        _isMoving = false;
        Vector3 impactPosition = _targetPosition;
        if (_particleTrail)
        {
            _particleTrail.StopTrail();
            _particleTrail = null;
        }

        NoaParticleEffect impactPrefab = _visualData.ParticleImpactPrefab;
        if (impactPrefab && Managers.UserSetting.CurrentSetting.IsEffectEnabled)
        {
            GameObject impactObject = _poolController.Get(impactPrefab.gameObject, _visualData.EffectPoolCapacity);
            if (impactObject && impactObject.TryGetComponent(out NoaParticleEffect impact))
                impact.PlayImpact(_poolController, impactPosition, _visualData.ImpactParticleMaterial, _visualData.ParticleColorMin, _visualData.ParticleColorMax, _visualData.ImpactEffectFacingDirection, _attackDirection);
        }

        PurifyPoolController poolController = _poolController;
        _poolController = null;
        poolController.Release(gameObject);
    }

    private void ApplyFacingDirection()
    {
        PurifyEffectFacingDirection facingDirection = _visualData.AttackEffectFacingDirection;
        if (facingDirection == PurifyEffectFacingDirection.None || _attackDirection.sqrMagnitude <= 0.0001f)
            return;

        Vector3 direction = facingDirection == PurifyEffectFacingDirection.Left ? -_attackDirection : _attackDirection;
        transform.right = direction.normalized;
    }

    private void ResetState()
    {
        _isMoving = false;
        _attackDirection = Vector3.zero;
        _targetPosition = Vector3.zero;
        if (_particleTrail)
        {
            _particleTrail.StopTrail();
            _particleTrail = null;
        }

        if (animatorEffect)
        {
            animatorEffect.enabled = false;
            animatorEffect.runtimeAnimatorController = null;
            animatorEffect.speed = 1f;
        }

        if (spriteRendererEffect)
        {
            spriteRendererEffect.sprite = null;
            spriteRendererEffect.enabled = true;
            spriteRendererEffect.color = Color.white;
        }

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
