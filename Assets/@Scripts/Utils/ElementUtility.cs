public enum ElementRelation
{
    Neutral,
    Advantage,
    Disadvantage
}

public static class ElementUtility
{
    private static readonly Define.ElementType[] _elements =
    {
        Define.ElementType.Water,
        Define.ElementType.Fire,
        Define.ElementType.Wind,
        Define.ElementType.Earth
    };

    public static Define.ElementType[] Elements => _elements;

    public static int GetIndex(Define.ElementType element)
    {
        for (int index = 0; index < _elements.Length; index++)
        {
            if (_elements[index] == element)
                return index;
        }

        return -1;
    }

    public static string GetName(Define.ElementType element)
    {
        switch (element)
        {
            case Define.ElementType.Water:
                return "물";
            case Define.ElementType.Fire:
                return "불";
            case Define.ElementType.Wind:
                return "바람";
            case Define.ElementType.Earth:
                return "땅";
            default:
                return string.Empty;
        }
    }

    public static ElementRelation GetRelation(Define.ElementType attackerElement, Define.ElementType defenderElement)
    {
        if (attackerElement == Define.ElementType.Neutral || defenderElement == Define.ElementType.Neutral || attackerElement == defenderElement)
            return ElementRelation.Neutral;

        if (HasAdvantage(attackerElement, defenderElement)) return ElementRelation.Advantage;
        if (HasAdvantage(defenderElement, attackerElement)) return ElementRelation.Disadvantage;
        return ElementRelation.Neutral;
    }

    public static float GetDamageMultiplier(
        Define.ElementType attackerElement,
        Define.ElementType defenderElement,
        float advantageMultiplier,
        float disadvantageMultiplier)
    {
        switch (GetRelation(attackerElement, defenderElement))
        {
            case ElementRelation.Advantage:
                return advantageMultiplier;
            case ElementRelation.Disadvantage:
                return disadvantageMultiplier;
            default:
                return 1f;
        }
    }

    private static bool HasAdvantage(Define.ElementType attackerElement, Define.ElementType defenderElement)
    {
        switch (attackerElement)
        {
            case Define.ElementType.Fire:
                return defenderElement == Define.ElementType.Water;
            case Define.ElementType.Water:
                return defenderElement == Define.ElementType.Earth;
            case Define.ElementType.Earth:
                return defenderElement == Define.ElementType.Wind;
            case Define.ElementType.Wind:
                return defenderElement == Define.ElementType.Fire;
            default:
                return false;
        }
    }
}
