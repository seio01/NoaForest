using UnityEngine;

[CreateAssetMenu(fileName = "StageSO", menuName = "Noa Forest/Stage/Stage")]
public class StageSO : ScriptableObject
{
    private const float MIN_DIFFICULTY_MULTIPLIER = 0.01f;

    [Header("Display")]
    [SerializeField] private Define.StageId stageId;
    [SerializeField] private Sprite spriteStage;
    [SerializeField] private Sprite spriteStageHome;
    [SerializeField] private string stageName;
    [TextArea(2, 5)]
    [SerializeField] private string stageDescription;

    [Header("Purify Difficulty")]
    [Min(MIN_DIFFICULTY_MULTIPLIER)]
    [SerializeField] private float moteHealthMultiplier = 1f;
    [Min(MIN_DIFFICULTY_MULTIPLIER)]
    [SerializeField] private float moteEscapeDamageMultiplier = 1f;
    [SerializeField] private PurifyWaveSetSO waveSet;

    public Define.StageId StageId => stageId;
    public Sprite SpriteStage => spriteStage;
    public Sprite SpriteStageHome => spriteStageHome;
    public string StageName => stageName;
    public string StageDescription => stageDescription;
    public float MoteHealthMultiplier => moteHealthMultiplier;
    public float MoteEscapeDamageMultiplier => moteEscapeDamageMultiplier;
    public PurifyWaveSetSO WaveSet => waveSet;

    private void OnValidate()
    {
        if (!waveSet)
        {
            Debug.LogError($"[StageSO] PurifyWaveSetSO is missing: {name}");
        }
    }
}
