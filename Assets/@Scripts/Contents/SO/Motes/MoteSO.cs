using UnityEngine;

[CreateAssetMenu(fileName = "MoteSO", menuName = "Noa Forest/Purify/Mote")]
public class MoteSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Define.ElementType element;
    [Min(0.01f)]
    [SerializeField] private float health;
    [Min(0.01f)]
    [SerializeField] private float moveSpeed;
    [Min(1)]
    [SerializeField] private int escapeDamage;
    [Min(0)]
    [SerializeField] private int killReward;
    [SerializeField] private RuntimeAnimatorController animatorControllerMote;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Define.ElementType Element => element;
    public float Health => health;
    public float MoveSpeed => moveSpeed;
    public int EscapeDamage => escapeDamage;
    public int KillReward => killReward;
    public RuntimeAnimatorController AnimatorControllerMote => animatorControllerMote;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.Log($"[MoteSO] Id is empty: {name}");
        }

        if (health <= 0f)
        {
            Debug.Log($"[MoteSO] Health must be greater than zero: {id}");
        }

        if (moveSpeed <= 0f)
        {
            Debug.Log($"[MoteSO] MoveSpeed must be greater than zero: {id}");
        }

        if (escapeDamage <= 0)
        {
            Debug.Log($"[MoteSO] EscapeDamage must be greater than zero: {id}");
        }

        if (killReward < 0)
        {
            Debug.Log($"[MoteSO] KillReward cannot be negative: {id}");
        }

        if (!animatorControllerMote)
        {
            Debug.LogError($"[MoteSO] AnimatorController is missing: {id}");
        }
    }
}
