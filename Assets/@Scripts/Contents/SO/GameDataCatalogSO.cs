using UnityEngine;

[CreateAssetMenu(fileName = "GameDataCatalogSO", menuName = "Noa Forest/Game Data Catalog")]
public class GameDataCatalogSO : ScriptableObject
{
    [SerializeField] private NoaGroupSO noas;
    [SerializeField] private StageGroupSO stages;
    [SerializeField] private MoteGroupSO motes;
    [SerializeField] private BlessingGroupSO blessings;
    [SerializeField] private PurifyBalanceSO purifyBalance;

    public NoaGroupSO Noas => noas;
    public StageGroupSO Stages => stages;
    public MoteGroupSO Motes => motes;
    public BlessingGroupSO Blessings => blessings;
    public PurifyBalanceSO PurifyBalance => purifyBalance;
}
