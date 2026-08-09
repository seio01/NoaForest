using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

//PurifyGameManager 전체 다 의존x -> 일부만 요구하도록 제한하기 위함
public interface IMoteTargetProvider
{
    Mote FindClosestMote(Vector3 origin, float range);
}

public class Noa : MonoBehaviour
{

    [SerializeField] private Transform transformVisualRoot;
    [SerializeField] private Transform transformAttackOrigin;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private NoaVisual noaVisual;
    private const int RANGE_SEGMENT_COUNT = 64;
    private Material _sharedRangeMaterial;
    private IMoteTargetProvider _targetProvider;
    private LineRenderer _lineRendererRange;
    private Coroutine _attackCoroutine;
    private float _purifyPower;
    private float _purifyInterval;
    private float _purifyRange;
    private float _elementAdvantageMultiplier;
    private float _elementDisadvantageMultiplier;
    private Mote _targetMote;
    private bool _isAttacking;

    public NoaSO Data { get; private set; }
    public Sprite Icon => Data ? Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, Data.IconId) : null;
    public float PurifyPower => _purifyPower;
    public float PurifyInterval => _purifyInterval;
    public float PurifyRange => _purifyRange;

    private void Awake()
    {
        if (noaVisual)
        {
            noaVisual.AttackReleased += HandleAttackReleased;
            noaVisual.AttackFinished += HandleAttackFinished;
        }
    }

    public bool SetData(NoaSO data, NoaVisualSO visualData, Sprite sprite, NoaStatsSO stats, int level, PurifyBlessingEffects blessingEffects, IMoteTargetProvider targetProvider, PurifyPoolController poolController)
    {
        if (!transformAttackOrigin)
        {
            return false;
        }

        NoaCalculatedStats? result = stats.GetCalculatedStats(data.Tier, level);
        if(result == null)
        {
            return false;
        }

        StopAttack();

        Data = data;
        NoaCalculatedStats calculatedStats = result.Value;
        _targetProvider = targetProvider;
        _purifyPower = blessingEffects.CalculatePurifyPower(calculatedStats.PurifyPower);
        _purifyInterval = blessingEffects.CalculatePurifyInterval(calculatedStats.PurifyInterval, stats.MinimumPurifyInterval);
        _purifyRange = calculatedStats.PurifyRange;
        _elementAdvantageMultiplier = stats.ElementAdvantageMultiplier;
        _elementDisadvantageMultiplier = stats.ElementDisadvantageMultiplier;

        if (!InitializeRangeIndicator())
        {
            Debug.LogError("[Noa] Failed to create the range indicator.");
            return false;
        }

        UpdateRangeIndicatorPositions();
        HideRangeIndicator();

        spriteRenderer.sprite = sprite;
        if (!noaVisual || !noaVisual.SetData(visualData, sprite, poolController))
        {
            Debug.LogError($"[Noa] Failed to initialize NoaVisual: {data.Id}");
            return false;
        }

        gameObject.name = $"Noa_{data.Id}";
        _attackCoroutine = StartCoroutine(AttackRoutine());
        return true;
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            ShowRangeIndicator();
            return;
        }

        HideRangeIndicator();
    }

    public void FacePosition(Vector3 targetPosition)
    {
        if (noaVisual)
            noaVisual.FacePosition(targetPosition);
    }

    public void SetRenderOrder(int renderOrder)
    {
        Vector3 localPosition = transformVisualRoot.localPosition;
        localPosition.z = -renderOrder * 0.001f;
        transformVisualRoot.localPosition = localPosition;
    }

    private IEnumerator AttackRoutine()
    {
        var attackWait = new WaitForSeconds(_purifyInterval);

        while (true)
        {
            yield return attackWait;
            TryAttackClosestMote();
        }
    }

    private void TryAttackClosestMote()
    {
        if (_isAttacking || Data == null || _targetProvider == null)
        {
            return;
        }

        Mote mote = _targetProvider.FindClosestMote(transformAttackOrigin.position, _purifyRange);
        if(mote == null)
        {
            return;
        }

        FacePosition(mote.transform.position);
        _targetMote = mote;
        _isAttacking = noaVisual.PlayAttack(_purifyInterval);
        if (!_isAttacking)
            ApplyPendingAttack();
    }

    private void HandleAttackReleased()
    {
        ApplyPendingAttack();
    }

    private void ApplyPendingAttack()
    {
        Mote targetMote = _targetMote;
        Vector3 attackOriginPosition = transformAttackOrigin.position;
        if (!targetMote || !targetMote.IsTargetable || (targetMote.transform.position - attackOriginPosition).sqrMagnitude > _purifyRange * _purifyRange)
            targetMote = _targetProvider?.FindClosestMote(attackOriginPosition, _purifyRange);

        _targetMote = null;
        if (!targetMote || !targetMote.IsTargetable)
            return;

        float elementMultiplier = ElementUtility.GetDamageMultiplier(
            Data.Element,
            targetMote.Element,
            _elementAdvantageMultiplier,
            _elementDisadvantageMultiplier);
        Vector3 targetPosition = targetMote.transform.position;
        FacePosition(targetPosition);
        targetMote.TakeDamage(_purifyPower * elementMultiplier);
        noaVisual.PlayAttackEffect(targetPosition);
    }

    private void HandleAttackFinished()
    {
        _isAttacking = false;
        _targetMote = null;
    }

    private void StopAttack()
    {
        if (_attackCoroutine == null)
        {
            return;
        }

        StopCoroutine(_attackCoroutine);
        _attackCoroutine = null;
        _isAttacking = false;
        _targetMote = null;
        noaVisual?.ResetVisual();
    }

    #region Range Indicator

    private bool InitializeRangeIndicator()
    {
        if (_lineRendererRange)
        {
            return true;
        }

        var indicatorObject = new GameObject("RangeIndicator");
        indicatorObject.transform.SetParent(transform, false);
        _lineRendererRange = indicatorObject.AddComponent<LineRenderer>();

        ConfigureRangeIndicator();
        HideRangeIndicator();
        return _lineRendererRange;
    }

    private void ConfigureRangeIndicator()
    {
        if (!_lineRendererRange)
        {
            Debug.LogError("[Noa] Range LineRenderer is missing.");
            return;
        }

        _lineRendererRange.enabled = false;
        _lineRendererRange.sharedMaterial = GetRangeMaterial();
        _lineRendererRange.useWorldSpace = true;
        _lineRendererRange.loop = true;
        _lineRendererRange.positionCount = RANGE_SEGMENT_COUNT;
        _lineRendererRange.startWidth = 0.035f;
        _lineRendererRange.endWidth = 0.035f;
        _lineRendererRange.startColor = new Color(0.2f, 0.85f, 1f, 0.8f);
        _lineRendererRange.endColor = new Color(0.2f, 0.85f, 1f, 0.8f);
        _lineRendererRange.numCornerVertices = 2;
        _lineRendererRange.numCapVertices = 2;
        _lineRendererRange.alignment = LineAlignment.View;
        _lineRendererRange.textureMode = LineTextureMode.Stretch;
        _lineRendererRange.shadowCastingMode = ShadowCastingMode.Off;
        _lineRendererRange.receiveShadows = false;
        _lineRendererRange.sortingOrder = 5;
    }

    private void ShowRangeIndicator()
    {
        if (!_lineRendererRange || _purifyRange <= 0f)
        {
            return;
        }

        UpdateRangeIndicatorPositions();
        _lineRendererRange.enabled = true;
    }

    private void HideRangeIndicator()
    {
        if (_lineRendererRange)
        {
            _lineRendererRange.enabled = false;
        }
    }

    private void UpdateRangeIndicatorPositions()
    {
        if (!_lineRendererRange || _purifyRange <= 0f)
        {
            return;
        }

        Vector3 center = transformAttackOrigin.position;
        float angleStep = Mathf.PI * 2f / RANGE_SEGMENT_COUNT;

        for (int index = 0; index < RANGE_SEGMENT_COUNT; index++)
        {
            float angle = angleStep * index;
            Vector3 position = center + new Vector3(
                Mathf.Cos(angle) * _purifyRange,
                Mathf.Sin(angle) * _purifyRange,
                0f);
            _lineRendererRange.SetPosition(index, position);
        }
    }

    private Material GetRangeMaterial()
    {
        if (_sharedRangeMaterial)
        {
            return _sharedRangeMaterial;
        }

        Shader shader = Shader.Find(
            "Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (!shader)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (!shader)
        {
            Debug.LogError("[Noa] Range shader was not found.");
            return null;
        }

        _sharedRangeMaterial = new Material(shader)
        {
            name = "M_NoaRange_Runtime",
            hideFlags = HideFlags.HideAndDontSave
        };
        return _sharedRangeMaterial;
    }

    #endregion

    private void OnDisable()
    {
        StopAttack();
        SetSelected(false);
    }

    private void OnDestroy()
    {
        if (!noaVisual)
            return;

        noaVisual.AttackReleased -= HandleAttackReleased;
        noaVisual.AttackFinished -= HandleAttackFinished;
    }
}
