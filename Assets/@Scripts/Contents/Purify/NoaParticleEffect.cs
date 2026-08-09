using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class NoaParticleEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem[] particleSystems = new ParticleSystem[0];
    [SerializeField] private ParticleSystem[] particleSystemsImpactColor = new ParticleSystem[0];
    [SerializeField] private ParticleSystemRenderer[] particleSystemRenderersImpactMaterial = new ParticleSystemRenderer[0];
    [SerializeField] private SpriteRenderer spriteRendererEffect;
    [SerializeField] private float floatDuration;
    [SerializeField] private bool isBurstAnimated;

    private PurifyPoolController _poolController;
    private Coroutine _releaseCoroutine;
    private Color _initialColor = Color.white;
    private Vector3 _initialScale = Vector3.one;
    private Material[] _initialParticleMaterials = new Material[0];

    public void Play(PurifyPoolController poolController, Vector3 position)
    {
        _poolController = poolController;
        transform.SetParent(null, true);
        transform.position = position;
        transform.rotation = Quaternion.identity;
        StartParticles();
        _releaseCoroutine = StartCoroutine(ReleaseRoutine(GetDuration()));
    }

    public void PlayStandalone()
    {
        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        _poolController = null;
        StartParticles();
    }

    public void PlayImpact(PurifyPoolController poolController, Vector3 position, Material material, Color colorMin, Color colorMax, PurifyEffectFacingDirection facingDirection, Vector3 attackDirection)
    {
        ApplyImpactColor(colorMin, colorMax);
        ApplyParticleMaterial(material);
        ApplyImpactFacingDirection(facingDirection, attackDirection);
        Play(poolController, position);
    }

    public void PlayTrail(PurifyPoolController poolController, Transform parent, Color colorMin, Color colorMax, float emissionRate)
    {
        _poolController = poolController;
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        ApplyTrailSettings(colorMin, colorMax, emissionRate);
        StartParticles();
    }

    public void StopTrail()
    {
        if (!_poolController)
            return;

        transform.SetParent(null, true);
        StopParticles(false);
        _releaseCoroutine = StartCoroutine(ReleaseRoutine(GetRemainingLifetime()));
    }

    public void OnGet()
    {
        ResetState();
    }

    public void OnRelease()
    {
        ResetState();
        _poolController = null;
    }

    private void StartParticles()
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (!particleSystem)
                continue;

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ApplyTrailSettings(Color colorMin, Color colorMax, float emissionRate)
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (!particleSystem)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(colorMin, colorMax);

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = Mathf.Max(0f, emissionRate);
        }
    }

    private void ApplyParticleMaterial(Material material)
    {
        if (!material)
            return;

        foreach (ParticleSystemRenderer particleRenderer in particleSystemRenderersImpactMaterial)
        {
            if (particleRenderer)
                particleRenderer.sharedMaterial = material;
        }
    }

    private void ApplyImpactColor(Color colorMin, Color colorMax)
    {
        foreach (ParticleSystem particleSystem in particleSystemsImpactColor)
        {
            if (!particleSystem)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(colorMin, colorMax);
        }
    }

    private void ApplyImpactFacingDirection(PurifyEffectFacingDirection facingDirection, Vector3 attackDirection)
    {
        if (facingDirection == PurifyEffectFacingDirection.None || Mathf.Abs(attackDirection.x) <= 0.001f)
            return;

        bool isEffectFacingLeft = facingDirection == PurifyEffectFacingDirection.Left;
        bool isAttackFacingLeft = attackDirection.x < 0f;
        if (isEffectFacingLeft == isAttackFacingLeft)
            return;

        Vector3 localScale = _initialScale;
        localScale.x = -localScale.x;
        transform.localScale = localScale;
    }

    private void StopParticles(bool clear)
    {
        ParticleSystemStopBehavior stopBehavior = clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem)
                particleSystem.Stop(true, stopBehavior);
        }
    }

    private IEnumerator ReleaseRoutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            if (isBurstAnimated && spriteRendererEffect)
            {
                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                transform.localScale = _initialScale * Mathf.Lerp(0.72f, 1f, Mathf.Min(normalizedTime * 3f, 1f));
                float alpha = normalizedTime < 0.55f ? 1f : 1f - (normalizedTime - 0.55f) / 0.45f;
                spriteRendererEffect.color = new Color(_initialColor.r, _initialColor.g, _initialColor.b, alpha);
            }

            yield return null;
        }

        _releaseCoroutine = null;
        PurifyPoolController poolController = _poolController;
        _poolController = null;
        if (poolController)
            poolController.Release(gameObject);
    }

    private float GetDuration()
    {
        if (floatDuration > 0f)
            return floatDuration;

        float duration = 0.1f;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (!particleSystem)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax);
        }

        return duration;
    }

    private float GetRemainingLifetime()
    {
        float lifetime = 0.1f;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (!particleSystem)
                continue;

            lifetime = Mathf.Max(lifetime, particleSystem.main.startLifetime.constantMax);
        }

        return lifetime;
    }

    private void ResetState()
    {
        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        StopParticles(true);
        if (spriteRendererEffect)
        {
            spriteRendererEffect.enabled = true;
            spriteRendererEffect.color = _initialColor;
        }

        for (int i = 0; i < particleSystemRenderersImpactMaterial.Length; i++)
        {
            if (particleSystemRenderersImpactMaterial[i])
                particleSystemRenderersImpactMaterial[i].sharedMaterial = _initialParticleMaterials[i];
        }

        transform.localScale = _initialScale;
        transform.localRotation = Quaternion.identity;
    }

    private void Awake()
    {
        _initialScale = transform.localScale;
        if (spriteRendererEffect)
            _initialColor = spriteRendererEffect.color;

        _initialParticleMaterials = new Material[particleSystemRenderersImpactMaterial.Length];
        for (int i = 0; i < particleSystemRenderersImpactMaterial.Length; i++)
        {
            if (particleSystemRenderersImpactMaterial[i])
                _initialParticleMaterials[i] = particleSystemRenderersImpactMaterial[i].sharedMaterial;
        }
    }
}
