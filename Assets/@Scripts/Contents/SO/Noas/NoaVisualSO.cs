using UnityEngine;

public enum PurifyEffectFacingDirection
{
    None,
    Right,
    Left
}

[CreateAssetMenu(fileName = "NoaVisualSO", menuName = "Noa Forest/Purify/Noa Visual")]
public class NoaVisualSO : ScriptableObject
{
    [Header("Noa")]
    [SerializeField] private RuntimeAnimatorController animatorControllerNoa;
    [SerializeField] private float floatAttackAnimationDuration = 7f / 15f;

    [Header("Attack Effect")]
    [SerializeField] private NoaAttackEffect attackEffectPrefab;
    [SerializeField] private Sprite spriteAttackEffect;
    [SerializeField] private RuntimeAnimatorController animatorControllerAttackEffect;
    [SerializeField] private NoaParticleEffect particleTrailPrefab;
    [SerializeField] private Color colorParticleMin = Color.white;
    [SerializeField] private Color colorParticleMax = Color.white;
    [SerializeField] private float floatTrailEmissionRate = 16f;
    [SerializeField] private NoaParticleEffect particleImpactPrefab;
    [SerializeField] private Material materialImpactParticle;
    [SerializeField] private float floatAttackEffectSpeed = 6.5f;
    [SerializeField] private float floatAttackEffectScale = 1f;
    [SerializeField] private PurifyEffectFacingDirection attackEffectFacingDirection;
    [SerializeField] private PurifyEffectFacingDirection impactEffectFacingDirection;
    [SerializeField] private int intEffectPoolCapacity = 32;

    [Header("Audio")]
    [SerializeField] private Define.AudioClip attackSfx;

    public RuntimeAnimatorController AnimatorControllerNoa => animatorControllerNoa;
    public float AttackAnimationDuration => floatAttackAnimationDuration;
    public NoaAttackEffect AttackEffectPrefab => attackEffectPrefab;
    public Sprite AttackEffectSprite => spriteAttackEffect;
    public RuntimeAnimatorController AnimatorControllerAttackEffect => animatorControllerAttackEffect;
    public NoaParticleEffect ParticleTrailPrefab => particleTrailPrefab;
    public Color ParticleColorMin => colorParticleMin;
    public Color ParticleColorMax => colorParticleMax;
    public float TrailEmissionRate => floatTrailEmissionRate;
    public NoaParticleEffect ParticleImpactPrefab => particleImpactPrefab;
    public Material ImpactParticleMaterial => materialImpactParticle;
    public float AttackEffectSpeed => floatAttackEffectSpeed;
    public float AttackEffectScale => floatAttackEffectScale;
    public PurifyEffectFacingDirection AttackEffectFacingDirection => attackEffectFacingDirection;
    public PurifyEffectFacingDirection ImpactEffectFacingDirection => impactEffectFacingDirection;
    public int EffectPoolCapacity => intEffectPoolCapacity;
    public Define.AudioClip AttackSfx => attackSfx;

    private void OnValidate()
    {
        floatAttackAnimationDuration = Mathf.Max(0.01f, floatAttackAnimationDuration);
        floatTrailEmissionRate = Mathf.Max(0f, floatTrailEmissionRate);
        floatAttackEffectSpeed = Mathf.Max(0.01f, floatAttackEffectSpeed);
        floatAttackEffectScale = Mathf.Max(0.01f, floatAttackEffectScale);
        intEffectPoolCapacity = Mathf.Max(1, intEffectPoolCapacity);
    }
}
