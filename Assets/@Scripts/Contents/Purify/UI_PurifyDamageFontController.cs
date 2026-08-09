using UnityEngine;

public class UI_PurifyDamageFontController : MonoBehaviour
{
    private const string DAMAGE_COLOR_HEX = "#DE5728";

    [Header("Dependencies")]
    [SerializeField] private PurifyGameManager purifyGameManager;
    [SerializeField] private PurifyPoolController poolController;
    [SerializeField] private Camera cameraMain;
    [SerializeField] private Canvas canvasMain;
    [SerializeField] private UI_DamageFont damageFontPrefab;

    [Header("Targets")]
    [SerializeField] private RectTransform rectDamageFontRoot;
    [SerializeField] private RectTransform rectEnergyTarget;
    [SerializeField] private RectTransform rectForestTarget;

    [Header("Position")]
    [SerializeField] private Vector2 moteOffset = new Vector2(0f, 55f);
    [SerializeField] private Vector2 energyOffset = new Vector2(0f, 30f);
    [SerializeField] private Vector2 forestOffset = new Vector2(0f, -360f);
    [SerializeField] private float randomHorizontalOffset = 24f;
    [SerializeField] private int maximumDamageFontCount = 48;

    private void OnEnable()
    {
        if (!purifyGameManager) return;

        purifyGameManager.MoteDamaged += HandleMoteDamaged;
        purifyGameManager.TreeDamaged += HandleTreeDamaged;
        purifyGameManager.TreeHealed += HandleTreeHealed;
        purifyGameManager.EnergyGained += HandleEnergyGained;
    }

    private void OnDisable()
    {
        if (!purifyGameManager) return;

        purifyGameManager.MoteDamaged -= HandleMoteDamaged;
        purifyGameManager.TreeDamaged -= HandleTreeDamaged;
        purifyGameManager.TreeHealed -= HandleTreeHealed;
        purifyGameManager.EnergyGained -= HandleEnergyGained;
    }

    private void HandleMoteDamaged(Vector3 worldPosition, float damage)
    {
        ShowDamage(damage, GetWorldLocalPosition(worldPosition, cameraMain), moteOffset);
    }

    private void HandleTreeDamaged(int damage)
    {
        ShowDamage(damage, GetTargetLocalPosition(rectForestTarget), forestOffset);
    }

    private void HandleTreeHealed(int amount)
    {
        ShowRecovery(amount, GetTargetLocalPosition(rectForestTarget), forestOffset);
    }

    private void HandleEnergyGained(int amount)
    {
        if (amount <= 0) return;

        Managers.Sound.Play(Define.AudioClip.EnergyGained, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);

        Vector2? localPosition = GetTargetLocalPosition(rectEnergyTarget);
        if (localPosition == null) return;

        UI_DamageFont damageFont = GetDamageFont(localPosition.Value + energyOffset);
        damageFont?.PlayCurrency($"+{amount:N0}", Define.CurrencyType.Energy, ReleaseDamageFont);
    }

    private void ShowDamage(float damage, Vector2? localPosition, Vector2 offset)
    {
        if (!Managers.UserSetting.CurrentSetting.IsDamageFontEnabled || damage <= 0f || localPosition == null) return;

        int displayedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));

        UI_DamageFont damageFont = GetDamageFont(localPosition.Value + GetRandomizedOffset(offset));
        damageFont?.Play($"<color={DAMAGE_COLOR_HEX}>-{displayedDamage:N0}</color>", null, ReleaseDamageFont);
    }

    private void ShowRecovery(int amount, Vector2? localPosition, Vector2 offset)
    {
        if (!Managers.UserSetting.CurrentSetting.IsDamageFontEnabled || amount <= 0 || localPosition == null) return;

        UI_DamageFont damageFont = GetDamageFont(localPosition.Value + GetRandomizedOffset(offset));
        damageFont?.Play($"<color={Constants.Olive2}>+{amount:N0}</color>", null, ReleaseDamageFont);
    }

    private UI_DamageFont GetDamageFont(Vector2 localPosition)
    {
        if (!poolController || !damageFontPrefab || !rectDamageFontRoot || maximumDamageFontCount <= 0) return null;

        GameObject instance = poolController.Get(damageFontPrefab.gameObject, maximumDamageFontCount);
        if (!instance || !instance.TryGetComponent(out UI_DamageFont damageFont))
        {
            Debug.LogError("[UI_PurifyDamageFontController] Failed to get a damage font instance.");
            return null;
        }

        RectTransform rectTransform = damageFont.RectTransform;
        rectTransform.SetParent(rectDamageFontRoot, false);
        rectTransform.SetAsLastSibling();
        rectTransform.anchoredPosition = localPosition;
        return damageFont;
    }

    private void ReleaseDamageFont(UI_DamageFont damageFont)
    {
        if (damageFont && poolController) poolController.Release(damageFont.gameObject);
    }

    private Vector2? GetWorldLocalPosition(Vector3 worldPosition, Camera positionCamera)
    {
        if (!positionCamera || !rectDamageFontRoot || !canvasMain) return null;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(positionCamera, worldPosition);
        Camera canvasCamera = canvasMain.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasMain.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectDamageFontRoot, screenPosition, canvasCamera, out Vector2 localPosition)
            ? localPosition
            : null;
    }

    private Vector2? GetTargetLocalPosition(RectTransform target)
    {
        if (!target || !rectDamageFontRoot || !canvasMain) return null;

        Camera canvasCamera = canvasMain.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasMain.worldCamera;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, target.position);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectDamageFontRoot, screenPosition, canvasCamera, out Vector2 localPosition)
            ? localPosition
            : null;
    }

    private Vector2 GetRandomizedOffset(Vector2 offset)
    {
        offset.x += Random.Range(-randomHorizontalOffset, randomHorizontalOffset);
        return offset;
    }

}
